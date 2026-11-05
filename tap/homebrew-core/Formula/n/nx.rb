class Nx < Formula
  desc "Smart, Fast and Extensible Build System"
  homepage "https://nx.dev"
  url "https://registry.npmjs.org/nx/-/nx-24.0.4.tgz"
  sha256 "b5d729470659abdf10e36787ae46582eb998d965e5ff8ee5f661240edd538a3c"
  license "MIT"
  version_scheme 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "0c340f551b4e3f9357391e32703b896aaa1e6030256f8e968e7e5c2eb227d313"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    # Avoid daemon and plugin worker sockets in the test sandbox.
    ENV["NX_DAEMON"] = "false"
    ENV["NX_ISOLATE_PLUGINS"] = "false"

    (testpath/"package.json").write <<~JSON
      {
        "name": "@acme/repo",
        "version": "0.0.1",
        "scripts": {
          "test": "echo 'Tests passed'"
        }
      }
    JSON

    system bin/"nx", "init", "--no-interactive"
    assert_path_exists testpath/"nx.json"

    output = shell_output("#{bin}/nx test").gsub(/\e\[[0-9;]*m/, "")
    assert_match "Successfully ran target test for project @acme/repo", output
  end
end
