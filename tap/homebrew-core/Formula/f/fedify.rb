class Fedify < Formula
  desc "CLI toolchain for Fedify"
  homepage "https://fedify.dev/cli"
  url "https://registry.npmjs.org/@fedify/cli/-/cli-2.4.4.tgz"
  sha256 "2ade580935fdef5738b1188ddc60e174612ec2eb04b284563e0ed177f509f3c1"
  license "MIT"
  head "https://github.com/fedify-dev/fedify.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "97ce56688a82fedaf0f31f5f6f7d9ce0c9dd58442a49a58d99f14046918fa869"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
    generate_completions_from_executable(bin/"fedify", "completions")
  end

  test do
    assert_match version.to_s, shell_output("NO_COLOR=1 #{bin}/fedify --version")

    json = shell_output "#{bin}/fedify lookup -e @homebrew@fosstodon.org"
    actor = JSON.parse(json)
    assert_equal "https://fosstodon.org/users/homebrew", actor.first["@id"]
  end
end
