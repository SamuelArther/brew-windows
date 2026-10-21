class Skills < Formula
  desc "Open agent skills ecosystem"
  homepage "https://skills.sh"
  url "https://registry.npmjs.org/skills/-/skills-1.7.4.tgz"
  sha256 "fee71731971e94d90bff2ee55f5556dc325e2f6f53ca0b5eff93ed22b9d838dd"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0a4ad16a7b3dd8a48a9a3fa2afb5a263d92e6c3e59b239e4447d18df3cc3b66b"
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
