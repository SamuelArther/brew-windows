class Shadcn < Formula
  desc "CLI for adding components to your project"
  homepage "https://ui.shadcn.com"
  url "https://registry.npmjs.org/shadcn/-/shadcn-4.22.2.tgz"
  sha256 "657fe32da117fe5743ad6e663acdf3cf596eb6d2600e51e3fcaa8b293717992b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "97de353d314409b1614ba4f1bacc10e2eb0b58d6a41a468a1066a75b900bbf97"
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
