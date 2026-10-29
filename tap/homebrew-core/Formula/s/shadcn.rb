class Shadcn < Formula
  desc "CLI for adding components to your project"
  homepage "https://ui.shadcn.com"
  url "https://registry.npmjs.org/shadcn/-/shadcn-4.23.0.tgz"
  sha256 "d47c1aaae4d274b0f4343ca61be34eac0976c81de3c79965c2ab9b4e5dc09e3b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "aff9aaab84278c5dee9fbccc5ad363df1c546cb8f5da7ffaf875f79663a0d2e5"
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
