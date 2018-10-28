namespace ArtAscii
{
	public static class CharSets
	{
		public enum Set {
			None = 0, Ascii = 1, CodePage437 = 2
		}

		public static char[] Get(Set which)
		{
			switch(which)
			{
			case Set.Ascii: return C_Ascii;
			case Set.CodePage437: return C_CodePage437;
			}
			return null;
		}

		static char[] C_Ascii = {
			/*010*/ ' ','!','"','#','$','%','&','\'','(',')','*','+',',','-','.','/',
			/*011*/ '0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?',
			/*100*/ '@','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
			/*101*/ 'P','Q','R','S','T','U','V','W','X','Y','Z','[','\\',']','^','_',
			/*110*/ '`','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
			/*111*/ 'p','q','r','s','t','u','v','w','x','y','z','{','|','}','~',''
		};

		static char[] C_CodePage437 = {
			/*0000*/ ' ','☺','☻','♥','♦','♣','♠','•','◘','○','◙','♂','♀','♪','♫','☼',
			/*0001*/ '►','◄','↕','‼','¶','§','▬','↨','↑','↓','→','←', '∟','↔','▲','▼',
			/*0010*/ ' ','!','"','#','$','%','&','\'','(',')','*','+',',','-','.','/',
			/*0011*/ '0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?',
			/*0100*/ '@','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
			/*0101*/ 'P','Q','R','S','T','U','V','W','X','Y','Z','[','\\',']','^','_',
			/*0110*/ '`','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
			/*0111*/ 'p','q','r','s','t','u','v','w','x','y','z','{','|','}','~','⌂',
			/*1000*/ 'Ç','ü','é','â','ä','à','å','ç','ê','ë','è','ï','î','ì','Ä','Å',
			/*1001*/ 'É','æ','Æ','ô','ö','ò','û','ù','ÿ','Ö','Ü','¢','£','¥','₧','ƒ',
			/*1010*/ 'á','í','ó','ú','ñ','Ñ','ª','º','¿','⌐','¬','½','¼','¡','«','»',
			/*1011*/ '░','▒','▓','│','┤','╡','╢','╖','╕','╣','║','╗','╝','╜','╛','┐',
			/*1100*/ '└','┴','┬','├','─','┼','╞','╟','╚','╔','╩','╦','╠','═','╬','╧',
			/*1101*/ '╨','╤','╥','╙','╘','╒','╓','╫','╪','┘','┌','█','▄','▌','▐','▀',
			/*1110*/ 'α','ß','Γ','π','Σ','σ','µ','τ','Φ','Θ','Ω','δ','∞','φ','ε','∩',
			/*1111*/ '≡','±','≥','≤','⌠','⌡','÷','≈','°','∙','·','√','ⁿ','²','■',' '
		};
	}
}

