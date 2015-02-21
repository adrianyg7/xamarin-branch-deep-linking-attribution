﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace BranchXamarinSDK
{
	public class BranchApplyReferralCodeRequest : BranchRequest
	{
		public class ReferralParams {
			public string app_id;
			public string device_fingerprint_id;
			public string identity_id;
			public string session_id;
			public string link_click_id;

			public ReferralParams() {
			}
		}

		readonly String Code;
		readonly IBranchReferralInterface Callback;
		readonly ReferralParams Params;

		public BranchApplyReferralCodeRequest (string code, IBranchReferralInterface callback) : base(BranchRequestType.REQUEST_APPLY_REFERRAL_CODE)
		{
			Params = new ReferralParams ();
			Params.app_id = Branch.GetInstance().AppKey;
			Params.session_id = Session.Current.Id;
			Params.identity_id = User.Current.Id;
			Params.device_fingerprint_id = Session.Current.DeviceFingerprintId;
			Code = code;
			Callback = callback;
		}

		override async public Task Execute() {
			try {
				InitClient();
				var inSettings = new JsonSerializerSettings();
				inSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
				String inBody = JsonConvert.SerializeObject(Params, inSettings);
				Branch.GetInstance().Log("Sending apply referral code request", "WEBAPI");
				HttpResponseMessage response = await Client.PostAsync ("v1/applycode/" + Code,
					new StringContent (inBody, System.Text.Encoding.UTF8, "application/json"));
				if (response.StatusCode == HttpStatusCode.OK) {
					if (Callback != null) {
						String body = await response.Content.ReadAsStringAsync();
						Branch.GetInstance().Log("Apply referral code completed successfully", "WEBAPI");

						var settings = new JsonSerializerSettings();
						var converterList = new List<JsonConverter>();
						converterList.Add(new DictionaryConverter());
						settings.Converters = converterList;
						Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(body, settings);

						object code;
						result.TryGetValue("referral_code", out code);

						if (Callback != null) {
							if (code != null) {
								Callback.ReferralCodeApplied(Code);
							} else {
								Callback.ReferralRequestError(new BranchError("Invalid Code Applied"));
							}
						}
					}
				} else {
					Branch.GetInstance().Log("Apply referral code failed with HTTP error: " + response.ReasonPhrase, "WEBAPI", 6);
					if (Callback != null) {
						Callback.ReferralRequestError(new BranchError(response.ReasonPhrase, Convert.ToInt32(response.StatusCode)));
					}
				}
			} catch (TaskCanceledException ex) {
				Branch.GetInstance().Log("Apply referral code timed out", "WEBAPI", 6);
				if (Callback != null) {
					Callback.ReferralRequestError (new BranchError ("Operation timed out", 1));
				}
			} catch (Exception ex) {
				Branch.GetInstance().Log("Exception sending apply referral code: " + ex.Message, "WEBAPI", 6);
				if (Callback != null) {
					Callback.ReferralRequestError (new BranchError ("Exception: " + ex.Message));
				}
			}
		}
	}
}
