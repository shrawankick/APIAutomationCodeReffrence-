busing System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Repository;
using Note = Repository.Model.Xsd.Note.NewDataSetNote;
using Contact = Repository.Model.Xsd.Contact.NewDataSetContact;

namespace Rest.Test
{
    public class NoteTest
    {
        private async Task<dynamic> CustomHeader()
        {
            var listNote = await Setup.NoteProcessor.GetAllAsync();

            return CustomHeader(listNote.Item2);
        }

        private dynamic CustomHeader(HttpResponseMessage response)
        {
            var paginationHeader = response
                .Headers
                .GetValues("X-Pagination")
                .First();
            var customHeaderObject = JsonConvert.DeserializeObject<dynamic>(paginationHeader);
            return customHeaderObject;
        }

        private async Task<int> TotalItems()
        {
            var customHeaderObject = await CustomHeader();
            return customHeaderObject.TotalCount;
        }

        private async Task<dynamic> FakeNoteForTest()
        {
            var fakeNoteToBeInserted = Generator.GenerateNotes();
            var fakeTuple = await Setup.NoteProcessor.InsertAsync(fakeNoteToBeInserted);

            var fakeitems = fakeTuple.Item1;
            return fakeitems;
        }

        private async Task<dynamic> FakecontactForTest()
        {
            var fakecontactToBeInserted = Generator.GenerateContacts();
            var fakeTuple = await Setup.ContactProcessor.InsertAsync(fakecontactToBeInserted);

            var fakeitems = fakeTuple.Item1;
            return fakeitems;
        }

        [Test, Category("Note")]
        public async Task GetAllItems()
        {
            var listTuple = await Setup.NoteProcessor.GetAllAsync();
            var list = listTuple.Item1;
            var response = listTuple.Item2;
            Assert.IsTrue(response.IsSuccessStatusCode, "response failed");

            //total Notes
            var totalCount = await TotalItems();
            var listCount = list.Count;

            if (totalCount <= 10)
            {
                Assert.AreEqual(listCount, totalCount, "ListCount and TotalCount are not equal");
            }
            else
            {
                Assert.AreEqual(10, listCount, "List count is below 10 ");
            }
        }


        [Test, Category("Note")]
        public async Task GetAnItem()
        {
            Note actualNote = await FakeNoteForTest();
            var listTuple = await Setup.NoteProcessor.GetAsync(actualNote.sid);
            var exceptedNoteitem = listTuple.Item1;
            var response = listTuple.Item2;
            Assert.AreEqual
                (actualNote.subject, exceptedNoteitem.subject, "Subject is not equal");
            Assert.AreEqual
                (actualNote.description, exceptedNoteitem.description, "description is not equal");
            Assert.AreEqual
               (actualNote.noteBookSid, exceptedNoteitem.noteBookSid, "noteBookSid is not equal");

            Assert.IsTrue(response.IsSuccessStatusCode, "response failed");
        }

        [Test, Category("Note")]
        public async Task FailedGetAnItem()
        {
            var listTuple = await Setup.NoteProcessor.GetAsync("XXXXXXXXXXXXXXXX");
            var response = listTuple.Item2;
            Assert.IsTrue(
                (response.StatusCode == HttpStatusCode.NotFound) ||
                (response.StatusCode == HttpStatusCode.BadRequest),
                "The return status code does not match");
        }

        [Test, Category("Note")]
        public async Task PostAnItem()
        {
            //arrange//
            Note noteToBeInserted = await FakeNoteForTest();
            //act 
            var insertTuple = await Setup.NoteProcessor.InsertAsync(noteToBeInserted);
            var response = insertTuple.Item2;
            var actualInserteditem = insertTuple.Item1;
            var getExpectedNote = await Setup.NoteProcessor.GetAsync(actualInserteditem.sid);
            var expectedItem = getExpectedNote.Item1;

            //assert 
            Assert.IsTrue(response.IsSuccessStatusCode, "response failed");
            Assert.AreEqual(actualInserteditem.subject, expectedItem.subject, "subject did not Macheded");
            Assert.AreEqual(actualInserteditem.description, expectedItem.description, "description name did not mached");
            Assert.AreEqual(actualInserteditem.noteBookSid, expectedItem.noteBookSid, "noteBookSid name did not mached");            
        }

        [Test, Category("Note")]
        public async Task FailedDeleteAnItem()
        {
            var tuple = await Setup.NoteProcessor.DeleteAsync("xxxxxxxxxxxxxx");
            var response = tuple.Item2;
            Assert.AreEqual(
                HttpStatusCode.NotFound,
                response.StatusCode,
                "The return status code does not match");
        }

        [Test, Category("Note")]
        public async Task DeleteAnItem()
        {
            var innitialtotalCount = await TotalItems();
            var noteListTuple = await Setup.NoteProcessor.GetAllAsync();
            var noteList = noteListTuple.Item1;
            if (noteList.Count <= 0)
            {
                Assert.Inconclusive("Note list is empty");
            }
            Note noteToBeInsertedAnddeleted = await FakeNoteForTest(); //Generator.GenerateNotes();


            var noteid = noteToBeInsertedAnddeleted.sid;
            var noteToBeDeleted = noteid; //we use this to be deleted 

            var deleteTuple = await Setup.NoteProcessor.DeleteAsync(noteToBeDeleted);
            var response = deleteTuple.Item2;
            Assert.IsTrue(response.IsSuccessStatusCode, "response failed");

            var listTuple = await Setup.NoteProcessor.GetAsync(noteid);
            var responseAfterDeleting = listTuple.Item2;
            Assert.IsFalse(responseAfterDeleting.IsSuccessStatusCode, "response is passed");


            var finalTotalCount = await TotalItems();
            Assert.AreEqual(innitialtotalCount, finalTotalCount);
        }

        [Test, Category("Note")]
        public async Task FailedUpdateAnItem()
        {
            var noteExpected = Generator.GenerateNoteToupdate();
            var updatedNote = await Setup.NoteProcessor.UpdateAsync(
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxx",
                noteExpected);
            var response = updatedNote.Item2;
            Assert.AreEqual(
                HttpStatusCode.InternalServerError,
                response.StatusCode,
                "The return status code does not match");
        }

        [Test, Category("Note")]
        public async Task UpdateAnItem()
        {
            //setup
            var generateNoteToupdate = Generator.GenerateNoteToupdate();

            //
            Note noteToBeInserted = await FakeNoteForTest(); //Generator.GenerateNotes();

            var noteIdToBeUpdate = noteToBeInserted.sid;

            var updatedNote = await Setup.NoteProcessor.UpdateAsync
                (noteIdToBeUpdate, generateNoteToupdate);

            var updatedNoteItems = updatedNote.Item1;
            var updatedNoteResponce = updatedNote.Item2;

            Assert.IsTrue(updatedNoteResponce.IsSuccessStatusCode, "responce Failed");

            //var updatedNoteid = updatedNote.Item1.sid;

            //act
            var actualNoteTuple = await Setup.NoteProcessor.GetAsync(noteIdToBeUpdate);

            var noteActual = actualNoteTuple.Item1;
            var response = actualNoteTuple.Item2;
            //assertion
            Assert.IsTrue(response.IsSuccessStatusCode, "response failed");

            Assert.AreEqual(generateNoteToupdate.subject, noteActual.subject, "Subject did not match");
            Assert.AreEqual(generateNoteToupdate.description, noteActual.description, "Description did not match");
            //Assert.AreEqual(generateNoteToupdate.noteBookSid, noteToBeInserted.noteBookSid, "noteBookSid did not match");

        }

        [Test, Category("Note")]
        public async Task TestPagenationForNote()
        {
            //set a page and page size to test...            
            var page = 1;
            var pageSize = 10;
            var noteBooksid = "0511-0305-0027-0E04-053C_43_3";
            var totalNotes = await TotalItems();
            var listTuple = await Setup.NoteProcessor.GetAllAsync();
            //NewDataSetNote Note = listTuple.Item1[0].sid;

            // Can we guarantee that there are more than 5 users and we can go to the 
            // next page?
            if (totalNotes <= pageSize)
            {
                Assert.Inconclusive("The number of total Notes is not sufficient to do this test");
                return;
            }

            // get the first page
            var listTuplePage = await Setup.NoteProcessor.GetAllAsync(
                $"?&age={page}&pageSize={pageSize}&notebookSid={noteBooksid}");
            // get the first Note id from this list that we will compare later

            var expectedNote = listTuple.Item1[0];
            var expectedId = expectedNote.sid;
            var expectedsubject = expectedNote.subject;

            // Now from the header get the next page url and make sure it exists
            var customHeaderObject = CustomHeader(listTuple.Item2);
            string nextPageUrl = customHeaderObject.NextPageLink;
            if (nextPageUrl == "null")
            {
                Assert.Fail("Next page does not exist which is an error");
                return;
            }

            // get the previous page url and make sure it does not exist
            string prevPageUrl = customHeaderObject.PrevPageLink;
            if (prevPageUrl != "null")
            {
                Assert.Fail("This is the first page so prev page url should have been null");
                return;
            }

            // Now call the next page url using the REST api. First we need to extract
            // the querystring
            var queryString = (new Uri(nextPageUrl)).Query;
            listTuple = await Setup.NoteProcessor.GetAllAsync(queryString);

            // Lets now make sure that the previous page exists
            customHeaderObject = CustomHeader(listTuple.Item2);
            prevPageUrl = customHeaderObject.PrevPageLink;
            if (prevPageUrl == "null")
            {
                Assert.Fail("This is the second page so prev page url should have been present");
                return;
            }

            // Finally get the previous page url from the header and go back to the first page
            // to make sure we got the same first item
            queryString = (new Uri(prevPageUrl)).Query;
            listTuple = await Setup.NoteProcessor.GetAllAsync(queryString);

            var actualNote = listTuple.Item1[0];

            var actualId = actualNote.sid;
            var actualsubject = actualNote.subject;

            Assert.AreEqual(expectedId, actualId, "The values did not match");
            Assert.AreEqual(expectedsubject, actualsubject, "Subject did not matched");
        }

        private async Task<Tuple<Note, Contact>> AddNoteToContact()
        {
            Contact insertedContact = await FakecontactForTest();
            var insertNoteinContactTuple = await Setup.NoteProcessor.InsertAsync(
                Generator.GenerateNoteThatToBeInsertedInParent(
                    18, insertedContact.sid));
            var note = insertNoteinContactTuple.Item1;
            Assert.IsTrue(insertNoteinContactTuple.Item2.IsSuccessStatusCode, "Note not inserted properly");
            return new Tuple<Note, Contact>(note, insertedContact);
        }

        [Test, Category("Note")]
        public async Task TestGetAllByParentObjectForNote()
        {
            var parentType = "OCServiceContact";

            var noteContact = await AddNoteToContact();

            // url for geting a Note in a single contact
            var listOfNotesInAContact = await Setup.NoteProcessor.GetAllAsync(
                $"?parentType={parentType}&parentSid={noteContact.Item2.sid}");

            var toFindTotalHeaderCount = listOfNotesInAContact.Item2;
            var countInHeaderIs = toFindTotalHeaderCount.Headers.GetValues("X-Pagination").First();
            var customHeaderObject = JsonConvert.DeserializeObject<dynamic>(countInHeaderIs);
            int expectedNotesCountinContact = customHeaderObject.TotalCount;

            Assert.AreEqual(
                expectedNotesCountinContact,
                listOfNotesInAContact.Item1.Count,
                $"Expected Note {expectedNotesCountinContact} but got {listOfNotesInAContact.Item1.Count}");
            Assert.AreEqual(
                noteContact.Item1.sid,
                listOfNotesInAContact.Item1[0].sid,
                $"Expected sid {noteContact.Item1.sid} but got {listOfNotesInAContact.Item1[0].sid}");
            Assert.AreEqual(
                noteContact.Item1.subject,
                listOfNotesInAContact.Item1[0].subject,
                $"Expected subject {noteContact.Item1.subject} but got {listOfNotesInAContact.Item1[0].subject}");
            Assert.AreEqual(
                noteContact.Item1.parentObjectSid,
                listOfNotesInAContact.Item1[0].parentObjectSid,
                $"Expected ParentSid {noteContact.Item1.parentObjectSid} but got {listOfNotesInAContact.Item1[0].parentObjectSid}");
        }

        [Test, Category("Note")]
        public async Task TestGetAllByParentTypeForNote()
        {
            var parentType = "OCServiceContact";

            var NoteContact = await AddNoteToContact();

            var expectedNoteSid = NoteContact.Item1.sid;

            var listOfNotesInContact = await Setup.NoteProcessor.GetAllAsync(
                $"?parentType={parentType}");

            var ToGetSid = listOfNotesInContact.Item1;
            // We can also check the count from the header
            Assert.IsTrue(
                (listOfNotesInContact.Item1.Count > 1),
                $"Expected more than 1 task but got {listOfNotesInContact.Item1.Count}");


            // Write an assert that goes through multiple pages and check if the expectedTaskSid is 
            // there in one of those pages. Or make another call to the task by making the page size
            // equal to the count (use linq query). compare sid like: 
            // listOfTasksInAContact.Exists(x => x.sid == expectedTaskSid))
        }

        //end
    }
}