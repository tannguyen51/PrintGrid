const fs = require("fs");

function esc(s) {
  const chars = ['\', '*', '+', '?', '^', '$', '{', '}', '(', ')', '|', '[', ']'];
  let r = s;
  for (const c of chars) {
    r = r.split(c).join('\' + c);
  }
  return r;
}

function replaceLabelValue(xml, labelText, newValue) {
  const escaped = esc(labelText);
  // Find: <w:t...>label</w:t></w:r></w:p></w:tc><w:tc>...<w:tcMar></w:tcPr><w:p><w:r><w:rPr>...</w:rPr><w:t>(empty)</w:t>
  const idx = xml.indexOf('>' + labelText + '</w:t>');
  if (idx === -1) return null;
  
  // Walk forward to find the empty value cell
  let searchPos = idx + labelText.length + 8; // skip past </w:t>
  // Pattern: ></w:r></w:p></w:tc><w:tc><w:tcPr>[...]content...</w:tcMar></w:tcPr><w:p><w:r><w:rPr>[...]content...</w:rPr><w:t attr="val"></w:t>
  const pattern = new RegExp(
    '</w:r></w:p></w:tc><w:tc><w:tcPr>[\s\S]*?</w:tcMar></w:tcPr>' +
    '<w:p><w:r><w:rPr>[\s\S]*?</w:rPr><w:t[^>]*>)(</w:t>)',
    's'
  );
  
  const afterLabel = xml.substring(searchPos);
  const match = pattern.exec(afterLabel);
  
  if (match) {
    const beforeEmpty = searchPos + match.index + match[1].length;
    const afterEmpty = beforeEmpty + 6; // length of '</w:t>'
    return xml.substring(0, beforeEmpty) + newValue + xml.substring(afterEmpty);
  }
  return null;
}

// ============================================================
// BM01 - Design Brief
// ============================================================
console.log("=== Processing BM01 ===");
let bm01 = fs.readFileSync('SWP490_R2_BM01_Design_Brief_1/word/document.xml', 'utf8');

for (const [label, value] of [
  ["Project code", "FA26SE249"],
  ["Supervisor", "Phucnt40@fpt.edu.vn"],
  ["Repository link (tag review-2)", "Tag: review-2"],
  ["Version", "R2-Nov"],
  ["Date", "09/11/2026"]
]) {
  const result = replaceLabelValue(bm01, label, value);
  console.log(result ? "  OK: " + label : "  FAIL: " + label);
  bm01 = result || bm01;
}
fs.writeFileSync('SWP490_R2_BM01_Design_Brief_1/word/document.xml', bm01, 'utf8');
console.log("BM01 done\n");

// ============================================================
// BM02
// ============================================================
console.log("=== Processing BM02 ===");
let bm02 = fs.readFileSync('SWP490_R2_BM02_Self_Review_Report_1/word/document.xml', 'utf8');
for (const [label, value] of [
  ["AI tool and model", "Claude Code"],
  ["Run date", "09/11/2026"],
  ["Repository tag / commit", "review-2"],
  ["Registration form version used", "v1.2"]
]) {
  const result = replaceLabelValue(bm02, label, value);
  console.log(result ? "  OK: " + label : "  FAIL: " + label);
  bm02 = result || bm02;
}
fs.writeFileSync('SWP490_R2_BM02_Self_Review_Report_1/word/document.xml', bm02, 'utf8');
console.log("BM02 done\n");

// ============================================================
// BM03
// ============================================================
console.log("=== Processing BM03 ===");
let bm03 = fs.readFileSync('SWP490_R2_BM03_Record_of_Changes_1/word/document.xml', 'utf8');
for (const [label, value] of [
  ["Current registration version", "v1.1"],
  ["Proposed version", "v1.2"]
]) {
  const result = replaceLabelValue(bm03, label, value);
  console.log(result ? "  OK: " + label : "  FAIL: " + label);
  bm03 = result || bm03;
}
fs.writeFileSync('SWP490_R2_BM03_Record_of_Changes_1/word/document.xml', bm03, 'utf8');
console.log("BM03 done\n");

// ============================================================
// BM04
// ============================================================
console.log("=== Processing BM04 ===");
let bm04 = fs.readFileSync('SWP490_R2_BM04_Review2_Commitment_1/word/document.xml', 'utf8');
for (const [label, value] of [
  ["Project code", "FA26SE249"],
  ["Supervisor", "Phucnt40@fpt.edu.vn"]
]) {
  const result = replaceLabelValue(bm04, label, value);
  console.log(result ? "  OK: " + label : "  FAIL: " + label);
  bm04 = result || bm04;
}
fs.writeFileSync('SWP490_R2_BM04_Review2_Commitment_1/word/document.xml', bm04, 'utf8');
console.log("BM04 done\n");

console.log("ALL HEADER REPLACEMENTS COMPLETE!");
