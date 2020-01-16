using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BuildingSmart.Utilities.Dictionary
{

	[DataContract]
	internal class ifdContexts
	{
		[DataMember] public IfdContext[] IfdContext;
	}

	[DataContract]
	internal class ResponseSearch
	{
		[DataMember] public IfdConcept[] IfdConcept;
	}

	[DataContract]
	internal class ResponseConceptInRelationship
	{
		[DataMember(Order = 0)] public IfdConceptInRelationship[] IfdConceptInRelationship;
	}

	[DataContract]
	internal class IfdConceptInRelationship : IfdConcept
	{
		[DataMember(Order = 0)] public IfdContext contexts;//??single??
		[DataMember(Order = 1)] public string relationshipType;
	}

	[DataContract]
	internal class IfdBase
	{
		[DataMember(Order = 0)] public string guid;
	}

	[DataContract]
	internal class IfdContext : IfdBase
	{
		[DataMember(Order = 0)] public IfdName[] fullNames;
		[DataMember(Order = 1)] public IfdDescription[] definitions;
		[DataMember(Order = 2)] public string status;
		[DataMember(Order = 3)] public string versionDate;
		[DataMember(Order = 4)] public string versionId;
		[DataMember(Order = 5)] public bool readOnly;
		[DataMember(Order = 6)] public bool restricted;
	}

	[DataContract]
	internal class IfdConcept : IfdBase
	{
		[DataMember(Order = 0)] public string versionId;
		[DataMember(Order = 1)] public string versionDate;
		[DataMember(Order = 2)] public string status;
		[DataMember(Order = 3)] public IfdName[] fullNames;
		[DataMember(Order = 4)] public IfdDescription[] definitions;
		[DataMember(Order = 5)] public IfdDescription[] comments;
		[DataMember(Order = 6)] public string conceptType;
		[DataMember(Order = 7)] public IfdName[] shortNames;
		[DataMember(Order = 8)] public IfdName[] lexemes;
		[DataMember(Order = 9)] public IfdIllustration[] illustrations;
		[DataMember(Order = 10)] public IfdOrganization owner;

		public override string ToString()
		{
			foreach (IfdName ifdName in this.fullNames)
			{
				if (ifdName.languageFamily == "IFC")
				{
					return ifdName.name;
				}
			}

			foreach (IfdName ifdName in this.fullNames)
			{
				if (ifdName.language.languageCode == "en")
				{
					return ifdName.name;
				}
			}

			return this.guid;
		}
	}

	[DataContract]
	internal class IfdIllustration : IfdLanguageRepresentationBase
	{
		[DataMember(Order = 0)] public string blobstoreKey;
		[DataMember(Order = 1)] public string illustrationUrl;
	}

	[DataContract]
	internal class IfdOrganization
	{
		[DataMember(Order = 0)] public string guid;
		[DataMember(Order = 1)] public string name;
		[DataMember(Order = 2)] public string url;
	}

	internal enum IfdConceptTypeEnum
	{
		NULL = 0,
		ACTOR = 1,
		ACTIVITY = 2,
		DOCUMENT = 3,
		PROPERTY = 4,
		SUBJECT = 5,
		UNIT = 6,
		MEASURE = 7,
		VALUE = 8,
		NEST = 9,
		BAG = 10,
		CLASSIFICATION = 11,
		UNDEFINED = -1,
	}

	internal enum IfdRelationshipTypeEnum
	{
		NULL = 0,
		COLLECTS = 1,
		ASSIGNS_COLLECTIONS = 2,
		ASSOCIATES = 3,
		COMPOSES = 4,
		GROUPS = 5,
		SPECIALIZES = 6,
		ACTS_UPON = 7,
		SEQUENCES = 8,
		DOCUMENTS = 9,
		CLASSIFIES = 10,
		ASSIGNS_MEASURES = 11,
		ASSIGNS_PROPERTIES = 12,
		ASSIGNS_UNITS = 13,
		ASSIGNS_VALUES = 14,
		ASSIGNS_PROPERTY_WITH_VALUES = 15,
		UNDEFINED = -1,
	}

	[DataContract]
	internal abstract class IfdLanguageRepresentationBase
	{
		[DataMember(Order = 0)] public string guid;
		[DataMember(Order = 1)] public IfdLanguage language;
		[DataMember(Order = 2)] public string languageFamily;

	}

	[DataContract]
	internal class IfdName : IfdLanguageRepresentationBase
	{
		[DataMember(Order = 3)] public string name;
		[DataMember(Order = 4)] public string nameType;
	}

	[DataContract]
	internal class IfdDescription : IfdLanguageRepresentationBase
	{
		[DataMember(Order = 3)] public string description;
		[DataMember(Order = 4)] public string descriptionType; // enum...
	}

	internal enum IfdDescriptionTypeEnum
	{
		NULL = 0,
		DEFINITION = 1,
		COMMENT = 2,
		UNDEFINED = -1,
	}

	[DataContract]
	internal class IfdLanguage
	{
		[DataMember(Order = 0)] public string guid;
		[DataMember(Order = 1)] public string nameInEnglish;
		[DataMember(Order = 2)] public string nameInSelf;
		[DataMember(Order = 3)] public string languageCode;
	}
}
