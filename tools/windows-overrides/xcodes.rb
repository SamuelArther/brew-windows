class Xcodes < Formula
  desc "Install and switch between multiple versions of Visual Studio (Windows' Xcode)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "xcodes", "xcodes"
  end

  test do
    assert_match "2.1.0", shell_output("#{bin}/xcodes version")
    list = shell_output("#{bin}/xcodes list --offline")
    assert_match "(Visual Studio 2022)", list
    assert_match "17.14", list
    ENV["XCENV_ROOT"] = testpath.to_s
    shell_output("#{bin}/xcodes installed")
    assert_match "Windows SDKs", shell_output("#{bin}/xcodes runtimes")
    assert_match "unknown subcommand", shell_output("#{bin}/xcodes frobnicate 2>&1", 2)
  end
end
