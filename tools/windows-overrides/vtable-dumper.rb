class VtableDumper < Formula
  desc "List contents of virtual tables in a shared library"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "vtable-dumper", "vtable-dumper"
  end

  test do
    dll = "#{ENV["SystemRoot"]}/System32/msvcp_win.dll"
    assert_match(/: \d+ entries/, shell_output("#{bin}/vtable-dumper #{dll}"))
    assert_match "Vtable for std::", shell_output("#{bin}/vtable-dumper #{dll}")
    assert_match "1.2", shell_output("#{bin}/vtable-dumper --dumpversion")
  end
end
