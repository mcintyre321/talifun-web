﻿using System.Web;
using NUnit.Framework;
using Rhino.Mocks;

namespace Talifun.Web.Tests.Http
{
    [TestFixture]
    public class SingleByteRangeResponseTests
    {
        #region SetContentLength
        [Test]
        public void SetContentLength_ContentLengthSet_Void()
        {
            //Arrange
            var httpResponse = MockRepository.GenerateMock<HttpResponseBase>();
            var httpResponseHeaderHelper = MockRepository.GenerateMock<IHttpResponseHeaderHelper>();
            var entity = MockRepository.GenerateMock<IEntity>();

            var startRange = 0l;
            var endRange = 499l;
            var rangeItem = new RangeItem
            {
                StartRange = startRange,
                EndRange = endRange
            };
            var contentLength = endRange - startRange + 1;
   
            httpResponseHeaderHelper.Expect(x => x.AppendHeader(httpResponse, SingleByteRangeResponse.HTTP_HEADER_CONTENT_LENGTH, contentLength.ToString()));

            //Act
            var singleByteRangeResponse = new SingleByteRangeResponse(httpResponseHeaderHelper, rangeItem);
            singleByteRangeResponse.SetContentLength(httpResponse, entity);

            //Assert
            httpResponseHeaderHelper.VerifyAllExpectations();
            httpResponse.VerifyAllExpectations();
        }

        #endregion

        #region SetOtherHeaders
        [Test]
        public void SetOtherHeaders_OtherHeadersSet_Void()
        {
            //Arrange
            var httpResponse = MockRepository.GenerateMock<HttpResponseBase>();
            var httpResponseHeaderHelper = MockRepository.GenerateMock<IHttpResponseHeaderHelper>();
            var entity = MockRepository.GenerateMock<IEntity>();

            var contentType = "image/gif";
            var contentLength = 1000l;

            var startRange = 0l;
            var endRange = 499l;
            var rangeItem = new RangeItem
            {
                StartRange = startRange,
                EndRange = endRange
            };

            entity.Stub(x => x.ContentType).Return(contentType);
            entity.Stub(x => x.ContentLength).Return(contentLength);

            httpResponse.Expect(x => x.ContentType = contentType);
            httpResponseHeaderHelper.Expect(x => x.AppendHeader(httpResponse, SingleByteRangeResponse.HTTP_HEADER_CONTENT_RANGE, SingleByteRangeResponse.BYTES + " " + startRange + "-" + endRange + "/" + contentLength));

            //Act
            var singleByteRangeResponse = new SingleByteRangeResponse(httpResponseHeaderHelper, rangeItem);
            singleByteRangeResponse.SetOtherHeaders(httpResponse, entity);

            //Assert
            httpResponseHeaderHelper.VerifyAllExpectations();
            httpResponse.VerifyAllExpectations();
        }

        #endregion

        #region SendBody
        [Test]
        public void SendBody_GetRequest_Void()
        {
            //Arrange
            var httpResponse = MockRepository.GenerateMock<HttpResponseBase>();
            var httpResponseHeaderHelper = MockRepository.GenerateMock<IHttpResponseHeaderHelper>();
            var transmitEntityStrategy = MockRepository.GenerateMock<ITransmitEntityStrategy>();
            var requestHttpMethod = HttpMethod.Get;

            var startRange = 0l;
            var endRange = 499l;
            var rangeItem = new RangeItem
            {
                StartRange = startRange,
                EndRange = endRange
            };
            var bytesToRead = endRange - startRange + 1;

            //Act
            var singleByteRangeResponse = new SingleByteRangeResponse(httpResponseHeaderHelper, rangeItem);
            singleByteRangeResponse.SendBody(requestHttpMethod, httpResponse, transmitEntityStrategy);

            //Assert
            transmitEntityStrategy.AssertWasCalled(x => x.Transmit(httpResponse, startRange, bytesToRead));
        }

        [Test]
        public void SendBody_HeadRequest_Void()
        {
            //Arrange
            var httpResponse = MockRepository.GenerateMock<HttpResponseBase>();
            var httpResponseHeaderHelper = MockRepository.GenerateMock<IHttpResponseHeaderHelper>();
            var transmitEntityStrategy = MockRepository.GenerateMock<ITransmitEntityStrategy>();
            var requestHttpMethod = HttpMethod.Head;

            var startRange = 0l;
            var endRange = 499l;
            var rangeItem = new RangeItem
            {
                StartRange = startRange,
                EndRange = endRange
            };

            var bytesToRead = endRange - startRange + 1;

            //Act
            var singleByteRangeResponse = new SingleByteRangeResponse(httpResponseHeaderHelper, rangeItem);
            singleByteRangeResponse.SendBody(requestHttpMethod, httpResponse, transmitEntityStrategy);

            //Assert
            transmitEntityStrategy.AssertWasNotCalled(x => x.Transmit(httpResponse, startRange, bytesToRead));
        }

        [Test]
        [ExpectedException]
        public void SendBody_NotAHeadOrGetRequest_Void()
        {
            //Arrange
            var httpResponse = MockRepository.GenerateMock<HttpResponseBase>();
            var httpResponseHeaderHelper = MockRepository.GenerateMock<IHttpResponseHeaderHelper>();
            var transmitEntityStrategy = MockRepository.GenerateMock<ITransmitEntityStrategy>();
            var requestHttpMethod = HttpMethod.Options;

            var startRange = 0l;
            var endRange = 499l;
            var rangeItem = new RangeItem
            {
                StartRange = startRange,
                EndRange = endRange
            };

            var bytesToRead = endRange - startRange + 1;

            //Act
            var singleByteRangeResponse = new SingleByteRangeResponse(httpResponseHeaderHelper, rangeItem);
            singleByteRangeResponse.SendBody(requestHttpMethod, httpResponse, transmitEntityStrategy);
        }

        #endregion
    }
}
