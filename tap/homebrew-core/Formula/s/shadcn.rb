class Shadcn < Formula
  desc "CLI for adding components to your project"
  homepage "https://ui.shadcn.com"
  url "https://registry.npmjs.org/shadcn/-/shadcn-4.23.1.tgz"
  sha256 "77b90293fed975ed3435d5a0ba9fa93663739703a446df465af6a22d9d1e18b7"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7efe03d57eb5af94ae26b66a1819fb41661996e52474fbb5e967f8858c35c55e"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/shadcn --version")

    pipe_output = pipe_output("#{bin}/shadcn init -d 2>&1", "brew\n")
    assert_match "Project initialization completed.", pipe_output
    assert_path_exists "#{testpath}/brew/components.json"
  end
end
