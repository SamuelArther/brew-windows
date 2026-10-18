class Openspec < Formula
  desc "Spec-driven development (SDD) for AI coding assistants"
  homepage "https://openspec.dev/"
  url "https://registry.npmjs.org/@fission-ai/openspec/-/openspec-1.14.3.tgz"
  sha256 "f7a4eaf9199036713edf331f1a781abebc4ed9bd3fbf5a8aa549f482749e14c2"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7806ab39672b3ff0abac66036ffb3f4c67c37e15effbe0299512423b9c371d49"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
    generate_completions_from_executable(bin/"openspec", "completion", "generate")
  end

  test do
    system bin/"openspec", "init", "--tools", "none"
    assert_path_exists testpath/"openspec/changes"
    assert_path_exists testpath/"openspec/specs"
  end
end
