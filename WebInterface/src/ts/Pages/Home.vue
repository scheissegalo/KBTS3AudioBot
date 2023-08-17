<template>
	<div>
		<section class="hero is-primary" style="margin-top:3em; margin-bottom:5em;">
			<div class="hero-body">
				<div class="container">
					<h1 class="title">North Industries - TS3Audiobot</h1>
					<h2 class="subtitle">You can recognize music bots by their orange-colored icon. Not all bots are music bots.

Drag one of the music bots into your channel, and you can post YouTube links that the bot will automatically download and play. To post links, you need at least Member privileges.</h2>
				</div>
			</div>
		</section>

		<div class="tile is-ancestor">
			<div class="tile">
				<div class="tile is-parent">
					<div class="tile is-child notification is-primary has-text-centered">
						<h1 class="title">Login</h1>
						<b-field>
							<div class="control is-expanded is-clearfix">
								<input
									class="input"
									:value="authUid"
									@input="authUidInput($event.target.value)"
									placeholder="Client Uid"
									type="text"
								/>
							</div>
							<p v-if="authUid.length != 0" class="control">
								<span class="button is-static">:</span>
							</p>
							<b-input
								v-if="authUid.length != 0"
								ref="authTokenField"
								v-model="authToken"
								placeholder="Auth token"
								type="password"
								expanded
							/>
						</b-field>
					</div>
				</div>
			</div>
		</div>

		<div v-if="logged_in" class="tile is-ancestor">
			<div class="tile">
				<router-link to="overview" tag="a" class="tile is-parent">
					<div class="tile is-child notification is-success has-text-centered">
						<b-icon icon="view-dashboard" size="is-large"></b-icon>
						<div class="content">Jump to the Infoboard</div>
					</div>
				</router-link>

				<router-link to="bots" tag="a" class="tile is-parent">
					<div class="tile is-child notification is-success has-text-centered">
						<b-icon icon="robot" size="is-large"></b-icon>
						<div class="content">Jump to your Bots overview</div>
					</div>
				</router-link>

			</div>
		</div>
	</div>
</template>

<script lang="ts">
import Vue from "vue";
import { jmerge, Api, Get } from "../Api";
import { Util } from "../Util";
import { ApiAuth } from "../ApiAuth";

export default Vue.extend({
	data() {
		return {
			authUid: "",
			authToken: "",
			logged_in: false
		};
	},
	created() {
		this.authStr = Get.AuthData.getFullAuth();
	},
	computed: {
		authStr: {
			get(): string {
				return this.authUid + ":" + this.authToken;
			},
			set(val: string) {
				if (!val.includes(":")) {
					this.authUid = val;
					this.authToken = "";
				} else {
					const split = val.split(":");
					this.authUid = split[0].replace(/:/g, "");
					this.authToken = split[1];
				}
			}
		}
	},
	methods: {
		authUidInput(val: string) {
			if (!val.includes(":")) {
				this.authUid = val;
			} else {
				const split = val.split(":");
				this.authUid = split[0].replace(/:/g, "");
				this.authToken = split[1];
				(this.$refs["authTokenField"] as HTMLElement).focus();
			}
		}
	},
	watch: {
		async authStr(val: string) {
			Get.AuthData = ApiAuth.Create(val);
			window.localStorage.setItem("api_auth", Get.AuthData.getFullAuth());

			const res = await jmerge().get();
			this.logged_in = Util.check(this, res, "Auth failed");
		}
	}
});
</script>
