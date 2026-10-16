class VtableDumper < Formula
  desc "List contents of virtual tables in a shared library"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
