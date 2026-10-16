class ApibuilderCli < Formula
  desc "Command-line interface to generate clients for api builder"
  homepage "https://www.apibuilder.io"
  url "https://github.com/apicollective/apibuilder-cli/archive/refs/tags/0.2.4.tar.gz"
  sha256 "80291a48e1637c24ac691b5e56061264b31342d35a560a05a6fcabfb847a46fd"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bde03843011bdd3b1e9bca1d4911886a22e7a82e4752539332cc756f1ee6c6b2"
  end

  uses_from_macos "ruby"

  def install
    system "./install.sh", prefix
  end

  test do
    (testpath/"config").write <<~EOS
      [default]
      token = abcd1234
    EOS

    assert_match "Profile default:",
                 shell_output("#{bin}/read-config --path config")
    assert_match "Could not find apibuilder configuration directory",
                 shell_output("#{bin}/apibuilder", 1)
  end
end
