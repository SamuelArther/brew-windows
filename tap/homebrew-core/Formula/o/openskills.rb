class Openskills < Formula
  desc "Universal skills loader for AI coding agents"
  homepage "https://github.com/numman-ali/openskills"
  url "https://registry.npmjs.org/openskills/-/openskills-1.5.0.tgz"
  sha256 "2aa0c31c2a09fad8c32705d519a5497aa671381189bde9d8f7911a7967a9d9bc"
  license "Apache-2.0"

  # see https://github.com/numman-ali/openskills/issues/94
  deprecate! date: "2026-10-30", because: :unmaintained
  disable! date: "2027-10-30", because: :unmaintained

  bottle do
    sha256 cellar: :any, x86_64_windows: "77688ae1f3794eb125b4fb9f59e7904d180deff9376e5bd191622901f9ab8456"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir["#{libexec}/bin/*"]
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/openskills --version")
    assert_match "No skills installed", shell_output("#{bin}/openskills list")
  end
end
