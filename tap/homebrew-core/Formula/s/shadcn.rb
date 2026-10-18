class Shadcn < Formula
  desc "CLI for adding components to your project"
  homepage "https://ui.shadcn.com"
  url "https://registry.npmjs.org/shadcn/-/shadcn-4.22.0.tgz"
  sha256 "51ea23db4e8d9cc1d3d56ff352a8fa60b70f3e125fcf5c563e585dcb4b0e7cf6"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "204b9a973611aeebaa4b17c5a58707016aac772fffc77a78c46964a08caefcc5"
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
