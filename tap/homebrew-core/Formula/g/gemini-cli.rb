class GeminiCli < Formula
  desc "Interact with Google Gemini AI models from the command-line"
  homepage "https://geminicli.com"
  url "https://registry.npmjs.org/@google/gemini-cli/-/gemini-cli-0.65.0.tgz"
  sha256 "97f1f7f1e7e189054d27edfb9c62c6a45cec15b678840dfdde3c0ccd87fab376"
  license "Apache-2.0"

  livecheck do
    url :stable
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "0bfa5b9e5c39cc6279cadc29f235a29ac51bb2e4a12ea76ac74b2eb6bca0e154"
  end

  deprecate! date: "2026-06-18", because: :unsupported, replacement_cask: "antigravity-cli"
  disable! date: "2026-12-18", because: :unsupported, replacement_cask: "antigravity-cli"

  depends_on "node"

  on_linux do
    depends_on "pkgconf" => :build
    depends_on "glib"
    depends_on "libsecret"
  end

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    node_modules = libexec/"lib/node_modules/@google/gemini-cli/node_modules"
    cd node_modules/"@github/keytar" do
      rm_r "prebuilds"
      system "npm", "run", "build"
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gemini --version")

    assert_match "Please set an Auth method", shell_output("#{bin}/gemini --prompt Homebrew 2>&1", 41)
  end
end
