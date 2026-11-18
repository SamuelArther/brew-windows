class Yamlresume < Formula
  desc "Resumes as code in YAML"
  homepage "https://github.com/yamlresume/yamlresume"
  url "https://registry.npmjs.org/yamlresume/-/yamlresume-0.18.1.tgz"
  sha256 "f5be97cd293c29d0f8786427c2072c1e3d9b0cf8913533061c094fe8c785da27"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "95f6ea53be123257d677d2efeee45fa44e3a06a1d9d7b1c89aa145651ba7cbfd"
  end

  depends_on "node"

  on_linux do
    depends_on "fontconfig" # for font-list to run fc-list
  end

  allow_network_access! :build

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    return unless OS.mac?

    # Replace prebuilt binary by compiling based on upstream build script:
    # https://github.com/oldj/node-font-list/blob/master/scripts/build-darwin.sh
    cd libexec/"lib/node_modules/yamlresume/node_modules/font-list/libs/darwin" do
      rm("fontlist")
      system ENV.cc, "fontlist.m", "-framework", "AppKit", "-framework", "Foundation", "-o", "fontlist"
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/yamlresume --version")

    system bin/"yamlresume", "new"
    assert_match "YAMLResume provides a builtin schema", (testpath/"resume.yml").read

    output = shell_output("#{bin}/yamlresume validate resume.yml")
    assert_match "Resume validation passed", output
  end
end
