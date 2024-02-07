using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;

namespace FHIR_Demo;

public class FhirService
{
    protected readonly FhirClient Client;

    public static readonly Dictionary<FhirServer, string> FhirServerUrls = new()
    {
        { FhirServer.Firely, "https://server.fire.ly/" },
        { FhirServer.HAPI, "http://hapi.fhir.org/baseR4/" },
    };

    #region Constructors

    /// <summary>
    /// Creates a new <see cref="FhirService"/> instance using the default Firely <see cref="FhirClient"/>
    /// </summary>
    /// <param name="fhirClient"></param>
    public FhirService() : this(FhirServer.Firely) { }

    /// <summary>
    /// Creates a new <see cref="FhirService"/> instance using for the predefined <paramref name="fhirServer"/>.
    /// </summary>
    /// <param name="fhirClient"></param>
    public FhirService(FhirServer fhirServer)
    { 
        Client = new FhirClient(FhirServerUrls[fhirServer])
        {
            Settings = new FhirClientSettings
            {
                PreferredFormat = ResourceFormat.Json,
                ReturnPreference = ReturnPreference.Representation,
            },
        };
    }

    /// <summary>
    /// Creates a new <see cref="FhirService"/> instance using the pre-initialized <paramref name="client"/>.
    /// </summary>
    /// <param name="client"></param>
    public FhirService(FhirClient client)
    {
        Client = client;
    }

    #endregion Constructors

    /// <summary>
    /// Gets a list of patients matching the given <paramref name="criteria"/>.
    /// </summary>
    /// <param name="criteria">The search criteria.</param>
    /// <param name="maxResults">The maximum number of records returned (default 20).</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public async Task<List<Patient>> GetPatientsAsync(string[]? criteria = null, int maxResults = 20)
    {
        Console.WriteLine("Fetching patients...");

        Bundle? patientBundle = await Client.SearchAsync<Patient>(criteria: criteria);

        if (patientBundle == null)
        {
            throw new ApplicationException("Response did not return a bundle.");
        }

        if (patientBundle.Total == 0)
        {
            string error = "No patients found matching search criteria.";

            List<string> codes = patientBundle.Entry
                .ByResourceType<OperationOutcome>()
                .SelectMany(outcome => outcome.Issue)
                .SelectMany(issue => issue.Details.Coding)
                .Select(coding => coding.Code)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .ToList();

            if (codes.Count > 0)
            {
                error += $" Code(s): {string.Join(",", codes)}";
            }

            Console.WriteLine(error);

            return [];
        }

        Console.WriteLine($"{patientBundle.Total} result(s) found.");

        int bundleCount = 0;

        List<Patient> result = [];

        while (patientBundle != null && patientBundle.Total > 0)
        {
            Console.WriteLine($"Processing Bundle {++bundleCount} ({patientBundle.Entry.Count} entries)");

            IEnumerable<Patient> blob = patientBundle.Entry
                .ByResourceType<Patient>()
                .Take(maxResults - result.Count);

            result.AddRange(blob);

            if (result.Count >= maxResults) break;

            patientBundle = await Client.ContinueAsync(patientBundle);
        }

        return result;
    }

    /// <summary>
    /// Creates a new <see cref="Patient"/> with the given parameters.
    /// </summary>
    /// <param name="family"></param>
    /// <param name="given"></param>
    /// <param name="dob"></param>
    /// <returns></returns>
    public async Task<Patient?> CreatePatientAsync(string family, string given, Date? dob = null)
    {
        Patient newPatient = new Patient
        {
            Name = new List<HumanName>
            {
                new HumanName
                {
                    Family = family,
                    Given = new List<string>
                    {
                        given,
                    },
                },
            },
            BirthDateElement = dob,
        };

        Patient? result = await Client.CreateAsync<Patient>(newPatient);;

        if (result != null) Console.WriteLine($"New patient created with Id '{result.Id}'.");

        return result;
    }

    /// <summary>
    /// Returns the <see cref="Patient"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<Patient?> ReadPatientAsync(string id) => Client.ReadAsync<Patient>($"Patient/{id}");

    /// <summary>
    /// Adds <paramref name="phone"/> as a new telecom contact point for the <paramref name="patient"/>.
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="phone"></param>
    /// <param name="use"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<Patient?> AddPhoneNumberAsync(Patient patient, string phone, ContactPoint.ContactPointUse use = ContactPoint.ContactPointUse.Mobile)
    {
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentNullException(nameof(phone));

        ContactPoint contact = new ContactPoint
        {
            System = ContactPoint.ContactPointSystem.Phone,
            Value = phone,
            Use = use,
        };

        patient.Telecom.Add(contact);

        Patient? result = await Client.UpdateAsync<Patient>(patient);

        if (result != null) Console.WriteLine($"Telephone '{phone}' added to patient '{result.Id}'.");

        return result;
    }

    /// <summary>
    /// Makes a FHIR server API call to update the <paramref name="patient"/>.
    /// </summary>
    /// <param name="patient"></param>
    /// <returns></returns>
    public async Task<Patient?> UpdatePatientAsync(Patient patient)
    {
        Patient? result = await Client.UpdateAsync<Patient>(patient);

        if (result != null) Console.WriteLine($"Patient '{result.Id}' updated.");

        return result;
    }

    /// <summary>
    /// Deletes the <see cref="Patient"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async System.Threading.Tasks.Task DeletePatientAsync(string id)
    {
        await Client.DeleteAsync($"Patient/{id}");

        Console.WriteLine($"Patient '{id}' deleted.");
    }
}

public enum FhirServer
{
    /// <summary>
    /// Indicates the https://fire.ly/ test server.
    /// </summary>
    Firely,

    /// <summary>
    /// Indicates the https://hapi.fhir.org/ test server.
    /// </summary>
    HAPI,
}
