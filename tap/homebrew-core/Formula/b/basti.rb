class Basti < Formula
  desc "Securely connect to RDS, Elasticache, and other AWS resources in VPCs"
  homepage "https://www.basti.app"
  url "https://registry.npmjs.org/basti/-/basti-1.8.1.tgz"
  sha256 "cfcda8ebe4f095a5b891a471174787d7ea07c5ea6c06fdd02e44803d42d5e358"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ab7e60d5fe24f3782a00e57c6de66f9b3099ed77f7230e65832ad1e2eaba817c"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    # Remove incompatible pre-built binary, session-manager-plugin
    node_modules = libexec/"lib/node_modules/basti/node_modules"
    node_modules.glob("basti-session-manager-binary-*/*").each do |f|
      next if f.arch == Hardware::CPU.arch

      rm f
    end

    generate_completions_from_executable(bin/"basti", "completion",
                                            shells:                 [:bash, :zsh],
                                            shell_parameter_format: :none)
  end

  test do
    output = shell_output("#{bin}/basti cleanup")
    assert_match "No Basti-managed resources found in your account", output

    assert_match version.to_s, shell_output("#{bin}/basti --version")
  end
end
