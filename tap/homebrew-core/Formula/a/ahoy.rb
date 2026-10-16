class Ahoy < Formula
  desc "Creates self documenting CLI programs from commands in YAML files"
  homepage "https://ahoy-cli.github.io/"
  url "https://github.com/ahoy-cli/ahoy/releases/download/v3.0.2/ahoy-windows-amd64.zip"
  sha256 "d039e7c7791fffc5577eef366591847fcd4f19625b3f56936706c3e2122ee467"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ahoy-cli/ahoy/archive/refs/tags/v3.0.2.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/ahoy-bin-windows-amd64.exe", "ahoy"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ahoy --version 2>&1")
  end
end
