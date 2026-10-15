class Glow < Formula
  desc "Render markdown on the CLI"
  homepage "https://github.com/charmbracelet/glow"
  version "3.0.0"
  url "https://github.com/charmbracelet/glow/releases/download/v3.0.0/glow_3.0.0_Windows_x86_64.zip"
  sha256 "d8907e07436ecbb6738bc0e3bbf8aa6159e351f95082e9db3c3fc4200086f809"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/glow.exe", "glow"
  end

  test do
    assert_match "glow version #{version}", shell_output("#{bin}/glow --version")
  end
end
