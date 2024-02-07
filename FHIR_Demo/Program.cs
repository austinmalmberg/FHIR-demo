using FHIR_Demo;
using Hl7.Fhir.Model;

void DisplayPatientResults(List<Patient> patients)
{
    for (int i = 0; i < patients.Count; i++)
    {
        Patient patient = patients[i];

        string formattedDob = patient.BirthDate;
        if (DateTime.TryParse(patient.BirthDate, out DateTime dob))
        {
            int age = DateTime.Today.Year - dob.Year;
            if (DateTime.Today.DayOfYear < dob.DayOfYear) age -= 1;    // less 1 if birthdate hasn't occurred this year

            if (age >= 1)
            {
                formattedDob = $"{patient.BirthDate} (Age {age})";
            }
            else
            {
                formattedDob = $"{patient.BirthDate} (Age {DateTime.Now.Month - dob.Month} month/s)";
            }
        }

        if (string.IsNullOrWhiteSpace(formattedDob)) formattedDob = "<unknown>";

        List<string> phones = patient.Telecom
            .Select(t => t.Value)
            .Where(phone => !string.IsNullOrWhiteSpace(phone))
            .ToList();

        if (phones.Count == 0) phones = ["<none>"];

        Console.WriteLine($"INDEX {i}");
        Console.WriteLine($" - Id:           {patient.Id}");
        Console.WriteLine($" - Name:         {patient.Name.FirstOrDefault()}");
        Console.WriteLine($" - Dob:          {formattedDob}");
        Console.WriteLine($" - Contact:      {string.Join(", ", phones)}");
    }
}

FhirService service = new FhirService();

/* Get patient list */
// List<Patient> patients = await GetPatientsAsync(fhirClient, ["name=Smith"]);
// DisplayPatientResults(patients);

/* Patient CRUD operations */
Patient? patient = await service.CreatePatientAsync("Ritis", "Arthur", dob: new Date(1970, 1, 1));
if (patient != null)
{
    await service.AddPhoneNumberAsync(patient, "867.5309");

    DisplayPatientResults([patient]);

    await service.DeletePatientAsync(patient.Id);
}

Console.WriteLine("Done. Press any key to continue...");
Console.Read();

