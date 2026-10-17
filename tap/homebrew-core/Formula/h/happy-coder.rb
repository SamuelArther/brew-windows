class HappyCoder < Formula
  desc "CLI for operating AI coding agents from mobile devices"
  homepage "https://happy.engineering"
  url "https://github.com/slopus/happy-cli/archive/refs/tags/v0.13.0.tar.gz"
  sha256 "42e1281f806aa2e604d684853213e4936f2424ea220e90f2cd6c06f4f37f297d"
  license "MIT"
  head "https://github.com/slopus/happy-cli.git", branch: "main"

  livecheck do
    skip "Newer versions use non-FOSS @anthropic-ai/claude-agent-sdk"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "adbc2b2e3866dcd973a40b2644242050cf901c9db1c7e04bfe281d0b6bbd631f"
  end

  # As of 1.1.5, happy has a required dependency on non-FOSS @anthropic-ai/claude-agent-sdk
  # Ref: https://github.com/slopus/happy/commit/aa0014e501fb7263ab4f80a2447e65a0d5079f5a
  deprecate! date: "2026-07-11", because: "uses non-FOSS @anthropic-ai/claude-agent-sdk since 1.1.5"
  disable! date: "2027-07-11", because: "uses non-FOSS @anthropic-ai/claude-agent-sdk since 1.1.5"

  depends_on "yarn" => :build
  depends_on "difftastic"
  depends_on "node"
  depends_on "ripgrep"

  def install
    # Remove bundled binary archives before build
    rm_r "tools/archives"

    system "yarn", "install", "--immutable", "--ignore-scripts"
    system "yarn", "build"
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    # Create tools/unpacked with symlinks to Homebrew versions
    unpacked = libexec/"lib/node_modules/happy-coder/tools/unpacked"
    unpacked.mkpath
    unpacked.install_symlink formula_opt_bin("difftastic")/"difft"
    unpacked.install_symlink formula_opt_bin("ripgrep")/"rg"
  end

  test do
    output = shell_output("#{bin}/happy doctor")
    assert_match version.to_s, output
    assert_match "Doctor diagnosis complete!", output
  end
end
