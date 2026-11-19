class SafCli < Formula
  desc "CLI for the MITRE Security Automation Framework (SAF)"
  homepage "https://saf-cli.mitre.org"
  url "https://registry.npmjs.org/@mitre/saf/-/saf-1.8.0.tgz"
  sha256 "0b2433ccc0a161176153f949a5c4b708686a83a4aeaf88216f211880574bfd98"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e4ebc6eccb4ec300160ce7698c6cff4359b5ff095f8eb6b12cad971c45eabf8a"
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
