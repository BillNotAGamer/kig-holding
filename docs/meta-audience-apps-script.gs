/**
 * KIG Holding – Meta audience sync (Google Apps Script).
 * Website pushes the full customer list once a day; this script rewrites the sheet.
 *
 * Setup (Google account truyenthuyetchamponghcm@gmail.com):
 * 1. Create a Google Sheet "KIG - Data khach Meta".
 * 2. Extensions > Apps Script, paste this file, set SECRET below (any long random text).
 * 3. Deploy > New deployment > Web app: Execute as "Me", Who has access "Anyone". Copy the /exec URL.
 * 4. Railway > Variables: LeadSync__WebhookUrl = the /exec URL, LeadSync__Secret = same SECRET.
 */
const SECRET = 'DOI_THANH_CHUOI_BI_MAT_CUA_BAN';
const SHEET_NAME = 'Meta Audience';

function doPost(e) {
  const body = JSON.parse(e.postData.contents || '{}');
  if (SECRET && body.secret !== SECRET) {
    return ContentService.createTextOutput('forbidden');
  }

  const header = body.header || ['email', 'phone', 'fn', 'ln', 'ct', 'country', 'value'];
  const rows = Array.isArray(body.rows) ? body.rows : [];

  const book = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = book.getSheetByName(SHEET_NAME) || book.insertSheet(SHEET_NAME);
  sheet.clearContents();
  sheet.getRange(1, 1, 1, header.length).setValues([header]);
  if (rows.length) {
    sheet.getRange(2, 1, rows.length, header.length).setNumberFormat('@').setValues(rows);
  }

  const log = book.getSheetByName('Sync log') || book.insertSheet('Sync log');
  log.appendRow([new Date(), rows.length]);

  return ContentService.createTextOutput('ok ' + rows.length);
}
