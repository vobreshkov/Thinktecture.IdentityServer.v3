﻿using BrockAllen.MembershipReboot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Thinktecture.IdentityServer.Admin.Core;
using Thinktecture.IdentityServer.Core;


namespace MembershipReboot.IdentityServer.Admin
{
    public class MembershipRebootUserManager : IUserManager, IDisposable
    {
        UserAccountService userAccountService;
        IUserAccountQuery query;
        IDisposable cleanup;

        public MembershipRebootUserManager(
            UserAccountService userAccountService, 
            IUserAccountQuery query,
            IDisposable cleanup)
        {
            if (userAccountService == null) throw new ArgumentNullException("userAccountService");
            if (query == null) throw new ArgumentNullException("query");

            this.userAccountService = userAccountService;
            this.query = query;
            this.cleanup = cleanup;
        }

        public void Dispose()
        {
            if (this.cleanup != null)
            {
                cleanup.Dispose();
                cleanup = null;
            }
        }

        public Task<UserManagerMetadata> GetMetadataAsync()
        {
            var claims = new ClaimMetadata[]
            {
                new ClaimMetadata{
                    ClaimType = Constants.ClaimTypes.Subject,
                    DisplayName = "Subject",
                }
            };

            return Task.FromResult(new UserManagerMetadata
            {
                UniqueIdentitiferClaimType = Constants.ClaimTypes.Subject,
                Claims = claims
            });
        }

        public Task<UserManagerResult<QueryResult>> QueryAsync(string filter, int start, int count)
        {
            int total;
            var users = query.Query(filter, start, count, out total);

            var result = new QueryResult();
            result.Start = start;
            result.Count = count;
            result.Total = total;
            result.Users = users.Select(x =>
            {
                var s = new UserSummary
                {
                    Subject = x.ID.ToString("D"),
                    Username = x.Username
                };
                if (!String.IsNullOrWhiteSpace(x.Email))
                {
                    s.Claims = new Claim[] { new Claim(Constants.ClaimTypes.Email, x.Email) };
                }
                return s;
            });

            return Task.FromResult(new UserManagerResult<QueryResult>(result));
        }

        public Task<UserManagerResult<CreateResult>> CreateAsync(string username, string password)
        {
            try
            {
                var acct = this.userAccountService.CreateAccount(username, password, null);
                return Task.FromResult(new UserManagerResult<CreateResult>(new CreateResult { Subject=acct.ID.ToString("D") }));
            }
            catch(ValidationException ex)
            {
                return Task.FromResult(new UserManagerResult<CreateResult>(ex.Message));
            }
        }

        public async Task<UserManagerResult> SetPasswordAsync(string id, string password)
        {
            Guid g;
            if (!Guid.TryParse(id, out g))
            {
                return new UserManagerResult("Invalid id");
            }

            try
            {
                this.userAccountService.SetPassword(g, password);
            }
            catch (ValidationException ex)
            {
                return new UserManagerResult(ex.Message);
            }

            return UserManagerResult.Success;
        }

        public async Task<UserManagerResult<UserResult>> GetUserAsync(string subject)
        {
            Guid g;
            if (!Guid.TryParse(subject, out g))
            {
                return new UserManagerResult<UserResult>("Invalid user.");
            }

            try
            {
                var acct = this.userAccountService.GetByID(g);
                if (acct == null)
                {
                    return new UserManagerResult<UserResult>("Invalid user.");
                }
                
                var user = new UserResult
                {
                    Subject = subject, 
                    Username = acct.Username
                };
                return new UserManagerResult<UserResult>(user);
            }
            catch (ValidationException ex)
            {
                return new UserManagerResult<UserResult>(ex.Message);
            }
        }
    }
}
