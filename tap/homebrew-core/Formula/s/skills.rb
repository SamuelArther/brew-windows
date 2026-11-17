class Skills < Formula
  desc "Open agent skills ecosystem"
  homepage "https://skills.sh"
  url "https://registry.npmjs.org/skills/-/skills-1.9.0.tgz"
  sha256 "a7b9ce3f1dbf508f9c2dd1b6ea0a5dbe281fb59e13f6282227146ab86591fa15"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "eff4616440eeb36d6e1e36a6dd55f5fe0cf588bbc52a3bf050a6eb4e72d0bed9"
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
