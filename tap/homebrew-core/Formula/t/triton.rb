class Triton < Formula
  desc "Joyent Triton CLI"
  homepage "https://www.npmjs.com/package/triton"
  url "https://registry.npmjs.org/triton/-/triton-7.18.1.tgz"
  sha256 "36804146e9df26ac633524c7d800500f02d9c0ba6b76eb9bd4b5738ca34cbc09"
  license "MPL-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1af4cc195ed4123fa93b9e3d8c1cab1f1b10f897b09ed7c90c8fb2946cdbc483"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
    generate_completions_from_executable(bin/"triton", "completion", shells: [:bash])
  end

  test do
    output = shell_output("#{bin}/triton profile ls")
    assert_match(/\ANAME  CURR  ACCOUNT  USER  URL$/, output)
  end
end
