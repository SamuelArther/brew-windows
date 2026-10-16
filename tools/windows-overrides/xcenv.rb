class Xcenv < Formula
  desc "Visual Studio (Windows' Xcode) version manager"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "xcenv", "xcenv"
  end

  test do
    ENV["XCENV_ROOT"] = testpath.to_s
    assert_match "XCENV_SHELL", shell_output("#{bin}/xcenv init -")
    shell_output("#{bin}/xcenv versions")
    system bin/"xcenv", "global", "system"
    assert_equal "system", (testpath/"version").read.strip
    assert_match "system (set by", shell_output("#{bin}/xcenv version")
    assert_match "not installed", shell_output("#{bin}/xcenv local 1.0 2>&1", 1)
  end
end
