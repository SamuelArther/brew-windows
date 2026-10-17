class SafCli < Formula
  desc "CLI for the MITRE Security Automation Framework (SAF)"
  homepage "https://saf-cli.mitre.org"
  url "https://registry.npmjs.org/@mitre/saf/-/saf-1.7.0.tgz"
  sha256 "001d6bdb2ca7462ac86b5544068f7d2bf415cda777c6ef6ce3e1bd83af31269a"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c66abe46a2020477d3a2c25ead6a0c862187fb67b11f45d3848f40577254fda3"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/saf --version")

    output = shell_output("#{bin}/saf scan")
    assert_match "Visit https://saf.mitre.org/#/validate to explore and run inspec profiles", output
  end
end
