class Hexo < Formula
  desc "Fast, simple & powerful blog framework"
  homepage "https://hexo.io/"
  url "https://registry.npmjs.org/hexo/-/hexo-8.1.2.tgz"
  sha256 "3ba063de1fb6ecfe5ec79d1f9ac3e886deb2f7c3f0b93fd063b8e1ef11ff97d2"
  license "MIT"
  head "https://github.com/hexojs/hexo.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e1fbf404419b99e3d31bb481bec68054b271e4550f6e4f06339e2b849880fa4a"
  end

  depends_on "node"

  def install
    mkdir_p libexec/"lib"
    system "npm", "install", *std_npm_args(ignore_scripts: false)
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    output = shell_output("#{bin}/hexo --help")
    assert_match "Usage: hexo <command>", output.strip

    output = shell_output("#{bin}/hexo init blog --no-install")
    assert_match "Cloning hexo-starter", output.strip
    assert_path_exists testpath/"blog/_config.yml"
  end
end
