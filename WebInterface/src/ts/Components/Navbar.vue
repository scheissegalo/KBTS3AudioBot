<template>
	<b-navbar class="has-shadow is-spaced is-fixed-top">
		<template slot="brand">
			<b-navbar-item
				class="notification is-primary"
				id="ts3ab-logo"
				tag="router-link"
				:to="{ path: '/' }"
			>TS3AudioBot</b-navbar-item>
		</template>
		<template slot="start">
			<b-navbar-item tag="router-link" :to="{ path: '/overview' }">
				<b-icon icon="view-dashboard"></b-icon>
				<span>Overview</span>
			</b-navbar-item>
			<b-navbar-item tag="router-link" :to="{ path: '/bots' }">
				<b-icon icon="robot"></b-icon>
				<span>Bots</span>
			</b-navbar-item>
		</template>

		<template slot="end">
			<b-tooltip label="Visit our Project" position="is-bottom">
				<b-navbar-item tag="a" href="https://github.com/scheissegalo/KBTS3AudioBot" target="_blank">
					<b-icon icon="github-circle"></b-icon>
					<span class="is-hidden-desktop">Github</span>
				</b-navbar-item>
			</b-tooltip>

			<b-tooltip label="Report a bug" position="is-bottom">
				<b-navbar-item
					tag="a"
					href="https://github.com/scheissegalo/KBTS3AudioBot/issues/new/choose"
					target="_blank"
				>
					<b-icon icon="bug"></b-icon>
					<span class="is-hidden-desktop">Report a bug</span>
				</b-navbar-item>
			</b-tooltip>

			<b-tooltip label="Chat or ask for help" position="is-bottom">
				<b-navbar-item
					tag="a"
					href="https://app.north-industries.com/#/user/@admin:chat.north-industries.com"
					target="_blank"
				>
					<b-icon icon="account-multiple"></b-icon>
					<span class="is-hidden-desktop">Chat or ask for help</span>
				</b-navbar-item>
			</b-tooltip>

			<b-tooltip label="Site Settings" position="is-bottom">
				<b-navbar-item tag="a" @click.native="openSiteSettings()">
					<b-icon icon="cog"></b-icon>
					<span class="is-hidden-desktop">Site Settings</span>
				</b-navbar-item>
			</b-tooltip>

			<b-tooltip label="Login" position="is-bottom">
				<b-navbar-item tag="router-link" :to="{ path: '/' }">
					<b-icon icon="shield-key"></b-icon>
					<span class="is-hidden-desktop">Login</span>
				</b-navbar-item>
			</b-tooltip>
		</template>
	</b-navbar>
</template>

<script lang="ts">
import Vue from "vue";
import SiteSettingsModal from "../Modals/SiteSettingsModal.vue";

export default Vue.extend({
	data() {
		return {};
	},
	methods: {
		openSiteSettings() {
			this.$buefy.modal.open({
				parent: this,
				component: SiteSettingsModal,
				hasModalCard: false
			});
		}
	},
	components: {
		SiteSettingsModal
	}
});
</script>

<style lang="less">
#ts3ab-logo {
	margin-bottom: 0;
}
</style>
