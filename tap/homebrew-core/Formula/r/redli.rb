class Redli < Formula
  desc "Humane alternative to redis-cli with TLS support"
  homepage "https://github.com/IBM-Cloud/redli"
  url "https://github.com/IBM-Cloud/redli/releases/download/v0.18.0/redli_0.18.0_windows_amd64.tar.gz"
  sha256 "bcdfb6b0c79f17ef85a9bc0c2bac8db4d750e86c7b6e19582f9dabd03fc111a7"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/IBM-Cloud/redli/archive/refs/tags/v0.18.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/redli.exe", "redli"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/redli --version 2>&1")
  end
end
