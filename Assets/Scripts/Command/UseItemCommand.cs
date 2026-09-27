/****************************************************
    文件：UseItemCommand.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using Code_01.System;
using QFramework;

namespace Code_01.Command
{
    public class UseItemCommand : AbstractCommand<bool>
    {
        private ItemData _data;
        public UseItemCommand() { }

        public UseItemCommand(ItemData data)
        {
            _data = data;
        }

        protected override bool OnExecute()
        {
            return this.GetSystem<PlayerEventSystem>().ChangeAll(_data);
        }
    }
}
