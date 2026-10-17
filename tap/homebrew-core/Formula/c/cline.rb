class Cline < Formula
  desc "AI-powered coding agent for complex work"
  homepage "https://cline.bot"
  url "https://registry.npmjs.org/cline/-/cline-3.0.3.tgz"
  sha256 "760e4c66e4ead40db61f89ad4433546a33507f8182e02d000cf1fe6809e635b3"
  license "Apache-2.0"

  livecheck do
    skip "Newer versions use non-FOSS @anthropic-ai/claude-agent-sdk"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "b25563809b6147a7dc0b843f46819bfd51a888fe258fed5d2c8377b30bdced60"
  end

  # As of 3.0.4, cline has a required dependency on non-FOSS @anthropic-ai/claude-agent-sdk
  # via cline -> @cline/llms -> ai-sdk-provider-claude-code -> @anthropic-ai/claude-agent-sdk
  #
  # Also, as of 3.x, npm just installs pre-built binaries so formula no longer meets policy.
  # Compiling binaries requires Bun which is not available in Homebrew/core.
  # Ref: https://github.com/cline/cline/blob/main/sdk/apps/cli/DISTRIBUTION.md
  deprecate! date: "2026-05-18", because: "uses non-FOSS @anthropic-ai/claude-agent-sdk and pre-built binaries"
  disable! date: "2027-05-18", because: "uses non-FOSS @anthropic-ai/claude-agent-sdk and pre-built binaries"

  depends_on "node"

  on_macos do
    depends_on maximum_macos: :tahoe # doesn't run on macOS 27 and unable to update
  end

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match "Unauthorized", shell_output("#{bin}/cline task --json --plan 'Hello World!'", 1)
  end
end
