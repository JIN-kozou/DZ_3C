/// <summary>
/// 由 <see cref="PlayerArmedPresentation"/> 在掏枪/收枪时间轴上驱动：实现本接口并挂到角色（或 Inspector 指定引用）即可接枪模显隐。
/// </summary>
public interface IArmedWeaponModelVisibility
{
    void SetWeaponModelVisible(bool visible);
}
