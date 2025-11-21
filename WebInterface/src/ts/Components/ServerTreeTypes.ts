import { CmdServerTreeChannel, CmdServerTreeUser } from "../ApiObjects";

export interface IChannelBuildNode {
	own: CmdServerTreeChannel;
	after?: IChannelBuildNode;
	children: IChannelBuildNode[];
	user: CmdServerTreeUser[];
}
