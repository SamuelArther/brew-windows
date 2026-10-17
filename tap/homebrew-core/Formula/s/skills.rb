class Skills < Formula
  desc "Open agent skills ecosystem"
  homepage "https://skills.sh"
  url "https://registry.npmjs.org/skills/-/skills-1.7.2.tgz"
  sha256 "e269699f1a9d06d3768cbf97d979c53e2d6e3ca1f98fc2ae1bfaf9cceeb1b617"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "937274f0c8954b044a7d7c699d0f0624bb73c703588d68823cbb68fc2a065f6a"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/skills --version")
    assert_match "No project skills found", shell_output("#{bin}/skills list")
    system bin/"skills", "init", "test-skill"
    assert_path_exists testpath/"test-skill/SKILL.md"
  end
end
