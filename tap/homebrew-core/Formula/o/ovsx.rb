class Ovsx < Formula
  desc "Command-line interface for Eclipse Open VSX"
  homepage "https://www.npmjs.com/package/ovsx"
  url "https://registry.npmjs.org/ovsx/-/ovsx-1.2.0.tgz"
  sha256 "c161a2730cd74021e087b7012e54070c438531abe51b2b2739390edefca9c59e"
  license "EPL-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8dc39c8f67b75688a22a62b26442a84b6edc878dbc16a70f87fa89cd112a937c"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir[libexec/"bin/*"]
  end

  test do
    error = shell_output("#{bin}/ovsx verify-pat 2>&1", 1)
    assert_match "Unable to read the namespace's name", error
  end
end
