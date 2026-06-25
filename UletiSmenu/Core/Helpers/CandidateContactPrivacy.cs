using Core.DTOs;

namespace Core.Helpers
{
    public static class CandidateContactPrivacy
    {
        public static void RedactApplicantContactInfo(ApplicationApplicantDTO applicant)
        {
            applicant.Email = string.Empty;
            applicant.PhoneNumber = string.Empty;
        }

        public static void RedactPublicProfileContactInfo(EmployeePublicProfileDTO profile)
        {
            profile.Email = string.Empty;
            profile.PhoneNumber = string.Empty;
        }
    }
}
