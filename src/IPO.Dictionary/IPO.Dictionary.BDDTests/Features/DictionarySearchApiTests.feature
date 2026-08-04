Feature: DictionarySearchApiTests

The Dictionary Search BDD tests
 
Scenario: Dictionary search requests get sent succesfully
	Given Requesting the following files to be searched
		| FileName  | DictionaryType |
		| test.docx | Profanity      |
		| test.odt  | Profanity      |
		| test.pdf  | Profanity      |
		| test.docx | Military       |
		| test.odt  | Military       |
		| test.pdf  | Military       |
	When apiUrl SearchDictionary requested
	Then the files to be searched are uploaded succesfully

Scenario: The dictionary search results get retrieved succesfully
	Given Requesting of the results for the file
		| ResultsId | FileName  | DictionaryType | HasMatch |
		| 123       | test.docx | Profanity      | true     |
		| 234       | test.odt  | Profanity      | true     |
		| 334       | test.pdf  | Profanity      | true     |
		| 1245      | test.docx | Military       | true     |
		| 1457      | test.odt  | Military       | true     |
		| 123457    | test.pdf  | Military       | true     |
		| 1237      | test.docx | Profanity      | false    |
		| 2347      | test.odt  | Profanity      | false    |
		| 3347      | test.pdf  | Profanity      | false    |
		| 12457     | test.docx | Military       | false    |
		| 14577     | test.odt  | Military       | false    |
		| 1234577   | test.pdf  | Military       | false    |
	When apiURL SearchResults/{id} for dictionary search results requested
	Then the dictionary search results are retrieved succesfully
