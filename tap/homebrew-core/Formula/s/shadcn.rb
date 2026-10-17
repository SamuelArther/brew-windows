class Shadcn < Formula
  desc "CLI for adding components to your project"
  homepage "https://ui.shadcn.com"
  url "https://registry.npmjs.org/shadcn/-/shadcn-4.21.5.tgz"
  sha256 "588f42db012fdd0edee6ddf0c1d79e0b94013188960d6a09c9e0c0e416bd2bab"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3d2c025b5af23c2b51675559cb612c743f3a86ea1ce8d6c0a3627f438a379422"
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
