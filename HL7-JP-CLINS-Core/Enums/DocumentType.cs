namespace HL7_JP_CLINS_Core.Enums
{
    /// <summary>
    /// Enumeration of JP-CLINS document types
    /// </summary>
    public enum DocumentType
    {
        /// <summary>
        /// Electronic referral document
        /// </summary>
        EReferral,

        /// <summary>
        /// Electronic discharge summary document
        /// </summary>
        EDischargeSummary,

        /// <summary>
        /// Electronic checkup/examination document
        /// </summary>
        ECheckup,

        /// <summary>
        /// Electronic prescription document
        /// </summary>
        EPrescription,

        /// <summary>
        /// Electronic instruction summary document
        /// </summary>
        EInstructionSummary
    }

    /// <summary>
    /// Document status enumeration
    /// </summary>
    public enum DocumentStatus
    {
        /// <summary>
        /// Document is in draft state
        /// </summary>
        Draft,

        /// <summary>
        /// Document is finalized
        /// </summary>
        Final,

        /// <summary>
        /// Document has been amended
        /// </summary>
        Amended,

        /// <summary>
        /// Document has been cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Document has been replaced by a newer version
        /// </summary>
        Replaced
    }

    /// <summary>
    /// Urgency levels for referrals
    /// </summary>
    public enum UrgencyLevel
    {
        /// <summary>
        /// Routine referral
        /// </summary>
        Routine,

        /// <summary>
        /// Urgent referral
        /// </summary>
        Urgent,

        /// <summary>
        /// As soon as possible
        /// </summary>
        ASAP,

        /// <summary>
        /// Immediate/STAT
        /// </summary>
        STAT
    }

    /// <summary>
    /// Discharge condition enumeration
    /// </summary>
    public enum DischargeCondition
    {
        /// <summary>
        /// Patient condition improved
        /// </summary>
        Improved,

        /// <summary>
        /// Patient condition stable
        /// </summary>
        Stable,

        /// <summary>
        /// Patient condition worsened
        /// </summary>
        Worsened,

        /// <summary>
        /// Patient deceased
        /// </summary>
        Deceased,

        /// <summary>
        /// Patient transferred to another facility
        /// </summary>
        Transferred
    }

    /// <summary>
    /// Health checkup certification status
    /// </summary>
    public enum CertificationStatus
    {
        /// <summary>
        /// Fit for work/activity
        /// </summary>
        FitForWork,

        /// <summary>
        /// Restricted activity/work
        /// </summary>
        Restricted,

        /// <summary>
        /// Unfit for work/activity
        /// </summary>
        Unfit,

        /// <summary>
        /// Requires further evaluation
        /// </summary>
        RequiresEvaluation
    }

    /// <summary>
    /// Types of health checkups
    /// </summary>
    public enum CheckupType
    {
        /// <summary>
        /// Annual health checkup
        /// </summary>
        Annual,

        /// <summary>
        /// Pre-employment checkup
        /// </summary>
        PreEmployment,

        /// <summary>
        /// Periodic health examination
        /// </summary>
        Periodic,

        /// <summary>
        /// Special health examination
        /// </summary>
        Special,

        /// <summary>
        /// Follow-up examination
        /// </summary>
        FollowUp,

        /// <summary>
        /// Occupational health checkup
        /// </summary>
        Occupational,

        /// <summary>
        /// Executive health checkup
        /// </summary>
        Executive
    }

    /// <summary>
    /// Gender enumeration following FHIR standard
    /// </summary>
    public enum Gender
    {
        /// <summary>
        /// Male gender
        /// </summary>
        Male,

        /// <summary>
        /// Female gender
        /// </summary>
        Female,

        /// <summary>
        /// Other gender
        /// </summary>
        Other,

        /// <summary>
        /// Unknown gender
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Address use enumeration
    /// </summary>
    public enum AddressUse
    {
        /// <summary>
        /// Home address
        /// </summary>
        Home,

        /// <summary>
        /// Work address
        /// </summary>
        Work,

        /// <summary>
        /// Temporary address
        /// </summary>
        Temp,

        /// <summary>
        /// Old/previous address
        /// </summary>
        Old,

        /// <summary>
        /// Billing address
        /// </summary>
        Billing
    }

    /// <summary>
    /// Contact point system enumeration
    /// </summary>
    public enum ContactPointSystem
    {
        /// <summary>
        /// Phone number
        /// </summary>
        Phone,

        /// <summary>
        /// Fax number
        /// </summary>
        Fax,

        /// <summary>
        /// Email address
        /// </summary>
        Email,

        /// <summary>
        /// Pager number
        /// </summary>
        Pager,

        /// <summary>
        /// URL/website
        /// </summary>
        URL,

        /// <summary>
        /// SMS number
        /// </summary>
        SMS,

        /// <summary>
        /// Other contact method
        /// </summary>
        Other
    }

    /// <summary>
    /// Contact point use enumeration
    /// </summary>
    public enum ContactPointUse
    {
        /// <summary>
        /// Home contact
        /// </summary>
        Home,

        /// <summary>
        /// Work contact
        /// </summary>
        Work,

        /// <summary>
        /// Temporary contact
        /// </summary>
        Temp,

        /// <summary>
        /// Old contact
        /// </summary>
        Old,

        /// <summary>
        /// Mobile contact
        /// </summary>
        Mobile
    }
}