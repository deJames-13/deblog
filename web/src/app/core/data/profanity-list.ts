/**
 * Word filtering engine for comment moderation
 */

export const BANNED_WORDS = [
  'damn',
  'hell',
  'crap',
  'idiot',
  'stupid',
  'fool',
  'scam',
  'hate',
  'trash',
  'asshole',
  'bastard',
  'bitch',
  'bullshit',
  'fuck',
  'shit',
  'dick',
  'cock',
  'pussy',
  'cunt',
  'nigger',
  'faggot',
  'retard',
  'kill',
  'murder',
  'terrorist',
  'porn',
  'casino',
  'viagra',
  'crypto pump',
  'buy followers',
];

export interface WordFilterResult {
  hasBadWords: boolean;
  detectedWords: string[];
  cleanText: string;
}

export function filterProfanity(text: string): WordFilterResult {
  if (!text) {
    return { hasBadWords: false, detectedWords: [], cleanText: '' };
  }

  const detected: string[] = [];
  let cleaned = text;

  // Check each banned word
  for (const word of BANNED_WORDS) {
    // Word boundary regex, case-insensitive
    const escaped = word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const regex = new RegExp(`\\b${escaped}\\b`, 'gi');

    if (regex.test(text)) {
      detected.push(word);
      cleaned = cleaned.replace(regex, '***');
    }
  }

  return {
    hasBadWords: detected.length > 0,
    detectedWords: Array.from(new Set(detected)),
    cleanText: cleaned,
  };
}
