"""Generate the versioned Serbia geography catalog from the official Address Register.

Source: Republic Geodetic Authority (RGZ), Serbian Address Register open data.
Dataset: https://data.gov.rs/sr/datasets/adresni-registar/

The output intentionally contains only canonical municipalities and settlements.
Street geometry is not persisted in this repository.
"""

from __future__ import annotations

import csv
import io
import json
import urllib.request
import zipfile
from pathlib import Path


csv.field_size_limit(10_000_000)

SOURCE_URL = (
    "https://download.geosrbija.rs/download-api/opendata-proxy/export"
    "?category=ar&layer=ulica_ar&geometry=true&fileName=ulica_csv&format=csv"
)
OUTPUT_PATH = (
    Path(__file__).resolve().parents[1]
    / "UletiSmenu"
    / "Infrastructure.Persistence"
    / "Data"
    / "serbia-geography.json"
)


def normalized(value: str | None) -> str:
    return (value or "").strip()


def display_name(value: str | None) -> str:
    return normalized(value).lower().title()


def main() -> None:
    with urllib.request.urlopen(SOURCE_URL, timeout=180) as response:
        archive_bytes = response.read()

    with zipfile.ZipFile(io.BytesIO(archive_bytes)) as archive:
        csv_name = next(name for name in archive.namelist() if name.lower().endswith(".csv"))
        with archive.open(csv_name) as raw:
            reader = csv.DictReader(io.TextIOWrapper(raw, encoding="utf-8-sig", newline=""))

            regions: dict[str, dict[str, str]] = {}
            cities: dict[str, dict[str, str]] = {}

            for row in reader:
                if normalized(row.get("retired")):
                    continue

                region_code = normalized(row.get("opstina_maticni_broj"))
                city_code = normalized(row.get("naselje_maticni_broj"))
                if not region_code or not city_code:
                    continue

                regions[region_code] = {
                    "code": region_code,
                    "name": display_name(row.get("opstina_ime_lat")),
                    "nativeName": display_name(row.get("opstina_ime")),
                }
                cities[city_code] = {
                    "code": city_code,
                    "regionCode": region_code,
                    "name": display_name(row.get("naselje_ime_lat")),
                    "nativeName": display_name(row.get("naselje_ime")),
                }

    catalog = {
        "source": {
            "name": "Address Register of the Republic of Serbia (RGZ)",
            "url": "https://data.gov.rs/sr/datasets/adresni-registar/",
            "downloadUrl": SOURCE_URL,
        },
        "country": {
            "code": "RS",
            "name": "Srbija",
            "nativeName": "Srbija",
        },
        "regions": sorted(regions.values(), key=lambda item: (item["name"], item["code"])),
        "cities": sorted(cities.values(), key=lambda item: (item["name"], item["code"])),
    }

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(
        json.dumps(catalog, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(
        f"Wrote {len(catalog['regions'])} regions and {len(catalog['cities'])} cities "
        f"to {OUTPUT_PATH}"
    )


if __name__ == "__main__":
    main()
