class Libabigail < Formula
  desc "ABI Generic Analysis and Instrumentation Library"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "abidw", "abidw"
    windows_exe "libexec/wintools.exe", "abidiff", "abidiff"
    windows_exe "libexec/wintools.exe", "abicompat", "abicompat"
    windows_exe "libexec/wintools.exe", "abilint", "abilint"
  end

  def caveats
    <<~EOS
      On Windows the tools work on PE binaries (DLLs, EXEs, drivers) at the symbol level:
      exports, imported DLLs and DLL names. Type-level comparison needs debug info that PE files
      don't carry (PDB files are not read).
    EOS
  end

  test do
    dll = "#{ENV["SystemRoot"]}/System32/version.dll"
    assert_match "<dependency name=", shell_output("#{bin}/abilint #{dll}")
    assert_match "GetFileVersionInfoW", shell_output("#{bin}/abidw #{dll}")
    system bin/"abidw", "--out-file", testpath/"version.abi", dll
    assert_equal "", shell_output("#{bin}/abidiff #{testpath}/version.abi #{dll}")
    shell_output("#{bin}/abilint --noout #{testpath}/version.abi")
    assert_match "two input files", shell_output("#{bin}/abidiff #{dll} 2>&1", 2)
  end
end
