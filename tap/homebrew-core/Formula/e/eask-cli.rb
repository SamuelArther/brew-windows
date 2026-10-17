class EaskCli < Formula
  desc "CLI for building, running, testing, and managing your Emacs Lisp dependencies"
  homepage "https://emacs-eask.github.io/"
  url "https://registry.npmjs.org/@emacs-eask/cli/-/cli-0.12.9.tgz"
  sha256 "c678ba145235cab6d53175acbdeeb89d73420a5bd5f078fe8a8bfa0139fe6f20"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "de10dd9dce7423fd42adccb4cb04e37d09535b4f0036dce9096a728b1ed8ab5d"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/eask --version")

    system bin/"eask", "create", "package", "test-project"
    assert_path_exists testpath/"test-project"
    assert_match "Emacs is not installed", shell_output("#{bin}/eask compile 2>&1")
  end
end
