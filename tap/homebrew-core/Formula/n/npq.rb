class Npq < Formula
  desc "Audit npm packages before you install them"
  homepage "https://github.com/lirantal/npq"
  url "https://registry.npmjs.org/npq/-/npq-3.27.5.tgz"
  sha256 "609289d02d5ecf4ba57e1e453c0cc12918b91dfd9cd089a72bd655bfa41ac42d"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5de8d66f3fc82968502950467d6ce7510e5605a48e70adebca91809588185a3f"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/npq --version")

    output = shell_output("#{bin}/npq install npq@3.5.3 --dry-run", 1)
    assert_match "Package Health - Detected an old package", output
  end
end
