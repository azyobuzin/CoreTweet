using System;
using System.Collections.Generic;
using System.Text;
using LibAzyotter.Connection;

namespace LibAzyotter
{
    public class TwitterClient
    {
        public TwitterClient(ITwitterConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
            this.connection = connection;
        }

        private ITwitterConnection connection;
        public ITwitterConnection Connection
        {
            get
            {
                return this.connection;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                this.connection = value;
            }
        }
    }
}
