using System;

namespace TigerBeetle
{
	public sealed class Currency
	{
		#region Fields

		public static readonly Currency AED = new(784, "AED", 2, "United Arab Emirates dirham");
		public static readonly Currency AFN = new(971, "AFN", 2, "Afghan afghani");
		public static readonly Currency ALL = new(008, "ALL", 2, "Albanian lek");
		public static readonly Currency AMD = new(051, "AMD", 2, "Armenian dram");
		public static readonly Currency ANG = new(532, "ANG", 2, "Netherlands Antillean guilder");
		public static readonly Currency AOA = new(973, "AOA", 2, "Angolan kwanza");
		public static readonly Currency ARS = new(032, "ARS", 2, "Argentine peso");
		public static readonly Currency AUD = new(036, "AUD", 2, "Australian dollar");
		public static readonly Currency AWG = new(533, "AWG", 2, "Aruban florin");
		public static readonly Currency AZN = new(944, "AZN", 2, "Azerbaijani manat");
		public static readonly Currency BAM = new(977, "BAM", 2, "Bosnia and Herzegovina convertible mark");
		public static readonly Currency BBD = new(052, "BBD", 2, "Barbados dollar");
		public static readonly Currency BDT = new(050, "BDT", 2, "Bangladeshi taka");
		public static readonly Currency BGN = new(975, "BGN", 2, "Bulgarian lev");
		public static readonly Currency BHD = new(048, "BHD", 3, "Bahraini dinar");
		public static readonly Currency BIF = new(108, "BIF", 0, "Burundian franc");
		public static readonly Currency BMD = new(060, "BMD", 2, "Bermudian dollar");
		public static readonly Currency BND = new(096, "BND", 2, "Brunei dollar");
		public static readonly Currency BOB = new(068, "BOB", 2, "Boliviano");
		public static readonly Currency BOV = new(984, "BOV", 2, "Bolivian Mvdol");
		public static readonly Currency BRL = new(986, "BRL", 2, "Brazilian real");
		public static readonly Currency BSD = new(044, "BSD", 2, "Bahamian dollar");
		public static readonly Currency BTN = new(064, "BTN", 2, "Bhutanese ngultrum");
		public static readonly Currency BWP = new(072, "BWP", 2, "Botswana pula");
		public static readonly Currency BYN = new(933, "BYN", 2, "Belarusian ruble");
		public static readonly Currency BZD = new(084, "BZD", 2, "Belize dollar");
		public static readonly Currency CAD = new(124, "CAD", 2, "Canadian dollar");
		public static readonly Currency CDF = new(976, "CDF", 2, "Congolese franc");
		public static readonly Currency CHE = new(947, "CHE", 2, "WIR euro (complementary currency)");
		public static readonly Currency CHF = new(756, "CHF", 2, "Swiss franc");
		public static readonly Currency CHW = new(948, "CHW", 2, "WIR franc (complementary currency)");
		public static readonly Currency CLF = new(990, "CLF", 4, "Unidad de Fomento (funds code)");
		public static readonly Currency CLP = new(152, "CLP", 0, "Chilean peso");
		public static readonly Currency CNY = new(156, "CNY", 2, "Chinese yuan");
		public static readonly Currency COP = new(170, "COP", 2, "Colombian peso");
		public static readonly Currency COU = new(970, "COU", 2, "Unidad de Valor Real (UVR)");
		public static readonly Currency CRC = new(188, "CRC", 2, "Costa Rican colon");
		public static readonly Currency CUC = new(931, "CUC", 2, "Cuban convertible peso");
		public static readonly Currency CUP = new(192, "CUP", 2, "Cuban peso");
		public static readonly Currency CVE = new(132, "CVE", 2, "Cape Verdean escudo");
		public static readonly Currency CZK = new(203, "CZK", 2, "Czech koruna");
		public static readonly Currency DJF = new(262, "DJF", 0, "Djiboutian franc");
		public static readonly Currency DKK = new(208, "DKK", 2, "Danish krone");
		public static readonly Currency DOP = new(214, "DOP", 2, "Dominican peso");
		public static readonly Currency DZD = new(012, "DZD", 2, "Algerian dinar");
		public static readonly Currency EGP = new(818, "EGP", 2, "Egyptian pound");
		public static readonly Currency ERN = new(232, "ERN", 2, "Eritrean nakfa");
		public static readonly Currency ETB = new(230, "ETB", 2, "Ethiopian birr");
		public static readonly Currency EUR = new(978, "EUR", 2, "Euro");
		public static readonly Currency FJD = new(242, "FJD", 2, "Fiji dollar");
		public static readonly Currency FKP = new(238, "FKP", 2, "Falkland Islands pound");
		public static readonly Currency GBP = new(826, "GBP", 2, "Pound sterling");
		public static readonly Currency GEL = new(981, "GEL", 2, "Georgian lari");
		public static readonly Currency GHS = new(936, "GHS", 2, "Ghanaian cedi");
		public static readonly Currency GIP = new(292, "GIP", 2, "Gibraltar pound");
		public static readonly Currency GMD = new(270, "GMD", 2, "Gambian dalasi");
		public static readonly Currency GNF = new(324, "GNF", 0, "Guinean franc");
		public static readonly Currency GTQ = new(320, "GTQ", 2, "Guatemalan quetzal");
		public static readonly Currency GYD = new(328, "GYD", 2, "Guyanese dollar");
		public static readonly Currency HKD = new(344, "HKD", 2, "Hong Kong dollar");
		public static readonly Currency HNL = new(340, "HNL", 2, "Honduran lempira");
		public static readonly Currency HRK = new(191, "HRK", 2, "Croatian kuna");
		public static readonly Currency HTG = new(332, "HTG", 2, "Haitian gourde");
		public static readonly Currency HUF = new(348, "HUF", 2, "Hungarian forint");
		public static readonly Currency IDR = new(360, "IDR", 2, "Indonesian rupiah");
		public static readonly Currency ILS = new(376, "ILS", 2, "Israeli new shekel");
		public static readonly Currency INR = new(356, "INR", 2, "Indian rupee");
		public static readonly Currency IQD = new(368, "IQD", 3, "Iraqi dinar");
		public static readonly Currency IRR = new(364, "IRR", 2, "Iranian rial");
		public static readonly Currency ISK = new(352, "ISK", 0, "Icelandic króna");
		public static readonly Currency JMD = new(388, "JMD", 2, "Jamaican dollar");
		public static readonly Currency JOD = new(400, "JOD", 3, "Jordanian dinar");
		public static readonly Currency JPY = new(392, "JPY", 0, "Japanese yen");
		public static readonly Currency KES = new(404, "KES", 2, "Kenyan shilling");
		public static readonly Currency KGS = new(417, "KGS", 2, "Kyrgyzstani som");
		public static readonly Currency KHR = new(116, "KHR", 2, "Cambodian riel");
		public static readonly Currency KMF = new(174, "KMF", 0, "Comoro franc");
		public static readonly Currency KPW = new(408, "KPW", 2, "North Korean won");
		public static readonly Currency KRW = new(410, "KRW", 0, "South Korean won");
		public static readonly Currency KWD = new(414, "KWD", 3, "Kuwaiti dinar");
		public static readonly Currency KYD = new(136, "KYD", 2, "Cayman Islands dollar");
		public static readonly Currency KZT = new(398, "KZT", 2, "Kazakhstani tenge");
		public static readonly Currency LAK = new(418, "LAK", 2, "Lao kip");
		public static readonly Currency LBP = new(422, "LBP", 2, "Lebanese pound");
		public static readonly Currency LKR = new(144, "LKR", 2, "Sri Lankan rupee");
		public static readonly Currency LRD = new(430, "LRD", 2, "Liberian dollar");
		public static readonly Currency LSL = new(426, "LSL", 2, "Lesotho loti");
		public static readonly Currency LYD = new(434, "LYD", 3, "Libyan dinar");
		public static readonly Currency MAD = new(504, "MAD", 2, "Moroccan dirham");
		public static readonly Currency MDL = new(498, "MDL", 2, "Moldovan leu");
		public static readonly Currency MGA = new(969, "MGA", 2, "Malagasy ariary");
		public static readonly Currency MKD = new(807, "MKD", 2, "Macedonian denar");
		public static readonly Currency MMK = new(104, "MMK", 2, "Myanmar kyat");
		public static readonly Currency MNT = new(496, "MNT", 2, "Mongolian tögrög");
		public static readonly Currency MOP = new(446, "MOP", 2, "Macanese pataca");
		public static readonly Currency MRU = new(929, "MRU", 2, "Mauritanian ouguiya");
		public static readonly Currency MUR = new(480, "MUR", 2, "Mauritian rupee");
		public static readonly Currency MVR = new(462, "MVR", 2, "Maldivian rufiyaa");
		public static readonly Currency MWK = new(454, "MWK", 2, "Malawian kwacha");
		public static readonly Currency MXN = new(484, "MXN", 2, "Mexican peso");
		public static readonly Currency MXV = new(979, "MXV", 2, "Mexican Unidad de Inversion (UDI)");
		public static readonly Currency MYR = new(458, "MYR", 2, "Malaysian ringgit");
		public static readonly Currency MZN = new(943, "MZN", 2, "Mozambican metical");
		public static readonly Currency NAD = new(516, "NAD", 2, "Namibian dollar");
		public static readonly Currency NGN = new(566, "NGN", 2, "Nigerian naira");
		public static readonly Currency NIO = new(558, "NIO", 2, "Nicaraguan córdoba");
		public static readonly Currency NOK = new(578, "NOK", 2, "Norwegian krone");
		public static readonly Currency NPR = new(524, "NPR", 2, "Nepalese rupee");
		public static readonly Currency NZD = new(554, "NZD", 2, "New Zealand dollar");
		public static readonly Currency OMR = new(512, "OMR", 3, "Omani rial");
		public static readonly Currency PAB = new(590, "PAB", 2, "Panamanian balboa");
		public static readonly Currency PEN = new(604, "PEN", 2, "Peruvian sol");
		public static readonly Currency PGK = new(598, "PGK", 2, "Papua New Guinean kina");
		public static readonly Currency PHP = new(608, "PHP", 2, "Philippine peso");
		public static readonly Currency PKR = new(586, "PKR", 2, "Pakistani rupee");
		public static readonly Currency PLN = new(985, "PLN", 2, "Polish złoty");
		public static readonly Currency PYG = new(600, "PYG", 0, "Paraguayan guaraní");
		public static readonly Currency QAR = new(634, "QAR", 2, "Qatari riyal");
		public static readonly Currency RON = new(946, "RON", 2, "Romanian leu");
		public static readonly Currency RSD = new(941, "RSD", 2, "Serbian dinar");
		public static readonly Currency RUB = new(643, "RUB", 2, "Russian ruble");
		public static readonly Currency RWF = new(646, "RWF", 0, "Rwandan franc");
		public static readonly Currency SAR = new(682, "SAR", 2, "Saudi riyal");
		public static readonly Currency SBD = new(090, "SBD", 2, "Solomon Islands dollar");
		public static readonly Currency SCR = new(690, "SCR", 2, "Seychelles rupee");
		public static readonly Currency SDG = new(938, "SDG", 2, "Sudanese pound");
		public static readonly Currency SEK = new(752, "SEK", 2, "Swedish krona");
		public static readonly Currency SGD = new(702, "SGD", 2, "Singapore dollar");
		public static readonly Currency SHP = new(654, "SHP", 2, "Saint Helena pound");
		public static readonly Currency SLL = new(694, "SLL", 2, "Sierra Leonean leone");
		public static readonly Currency SOS = new(706, "SOS", 2, "Somali shilling");
		public static readonly Currency SRD = new(968, "SRD", 2, "Surinamese dollar");
		public static readonly Currency SSP = new(728, "SSP", 2, "South Sudanese pound");
		public static readonly Currency STN = new(930, "STN", 2, "São Tomé and Príncipe dobra");
		public static readonly Currency SVC = new(222, "SVC", 2, "Salvadoran colón");
		public static readonly Currency SYP = new(760, "SYP", 2, "Syrian pound");
		public static readonly Currency SZL = new(748, "SZL", 2, "Swazi lilangeni");
		public static readonly Currency THB = new(764, "THB", 2, "Thai baht");
		public static readonly Currency TJS = new(972, "TJS", 2, "Tajikistani somoni");
		public static readonly Currency TMT = new(934, "TMT", 2, "Turkmenistan manat");
		public static readonly Currency TND = new(788, "TND", 3, "Tunisian dinar");
		public static readonly Currency TOP = new(776, "TOP", 2, "Tongan paʻanga");
		public static readonly Currency TRY = new(949, "TRY", 2, "Turkish lira");
		public static readonly Currency TTD = new(780, "TTD", 2, "Trinidad and Tobago dollar");
		public static readonly Currency TWD = new(901, "TWD", 2, "New Taiwan dollar");
		public static readonly Currency TZS = new(834, "TZS", 2, "Tanzanian shilling");
		public static readonly Currency UAH = new(980, "UAH", 2, "Ukrainian hryvnia");
		public static readonly Currency UGX = new(800, "UGX", 0, "Ugandan shilling");
		public static readonly Currency USD = new(840, "USD", 2, "United States dollar");
		public static readonly Currency USN = new(997, "USN", 2, "United States dollar (next day)");
		public static readonly Currency UYI = new(940, "UYI", 0, "Uruguay Peso en Unidades Indexadas (URUIURUI)");
		public static readonly Currency UYU = new(858, "UYU", 2, "Uruguayan peso");
		public static readonly Currency UYW = new(927, "UYW", 4, "Unidad previsional");
		public static readonly Currency UZS = new(860, "UZS", 2, "Uzbekistan");
		public static readonly Currency VED = new(926, "VED", 2, "Venezuelan bolívar digital");
		public static readonly Currency VES = new(928, "VES", 2, "Venezuelan bolívar soberano");
		public static readonly Currency VND = new(704, "VND", 0, "Vietnamese đồng");
		public static readonly Currency VUV = new(548, "VUV", 0, "Vanuatu vatu");
		public static readonly Currency WST = new(882, "WST", 2, "Samoan tala");
		public static readonly Currency XAF = new(950, "XAF", 0, "CFA franc BEAC");
		public static readonly Currency XAG = new(961, "XAG", 0, "Silver (one troy ounce)");
		public static readonly Currency XAU = new(959, "XAU", 0, "Gold (one troy ounce)");
		public static readonly Currency XBA = new(955, "XBA", 0, "European Composite Unit (EURCO)");
		public static readonly Currency XBB = new(956, "XBB", 0, "European Monetary Unit (E.M.U.-6)");
		public static readonly Currency XBC = new(957, "XBC", 0, "European Unit of Account 9 (E.U.A.-9)");
		public static readonly Currency XBD = new(958, "XBD", 0, "European Unit of Account 17 (E.U.A.-17)");
		public static readonly Currency XCD = new(951, "XCD", 2, "East Caribbean dollar");
		public static readonly Currency XDR = new(960, "XDR", 0, "Special drawing rights");
		public static readonly Currency XOF = new(952, "XOF", 0, "CFA franc BCEAO");
		public static readonly Currency XPD = new(964, "XPD", 0, "Palladium (one troy ounce)");
		public static readonly Currency XPF = new(953, "XPF", 0, "CFP franc");
		public static readonly Currency XPT = new(962, "XPT", 0, "Platinum (one troy ounce)");
		public static readonly Currency XSU = new(994, "XSU", 0, "SUCRE");
		public static readonly Currency XTS = new(963, "XTS", 0, "Code reserved for testing");
		public static readonly Currency XUA = new(965, "XUA", 0, "ADB Unit of Account");
		public static readonly Currency XXX = new(999, "XXX", 0, "No currency");
		public static readonly Currency YER = new(886, "YER", 2, "Yemeni rial");
		public static readonly Currency ZAR = new(710, "ZAR", 2, "South African rand");
		public static readonly Currency ZMW = new(967, "ZMW", 2, "Zambian kwacha");
		public static readonly Currency ZWL = new(932, "ZWL", 2, "Zimbabwean dollar");

		private readonly decimal factor;

		#endregion Fields

		#region Constructor

		public Currency(ushort number, string code, byte decimalPlaces, string description)
		{
			factor = (decimal)Math.Pow(10, decimalPlaces);

			Name = code;
			Code = number;
			DecimalPlaces = (int)decimalPlaces;
			Description = description;
		}

		#endregion Constructor

		#region Properties

		public ushort Code { get; }

		public string Name { get; }

		public int DecimalPlaces { get; }

		public string Description { get; }

		#endregion Properties

		#region Methods

		public override string ToString()
		{
			return $"{Name} {Description}";
		}

		public ulong ToUInt64(decimal value) => (ulong)(value * factor);

		public decimal FromUInt64(ulong value) => Math.Round(value / factor, DecimalPlaces);

		#endregion Methods
	}
}
