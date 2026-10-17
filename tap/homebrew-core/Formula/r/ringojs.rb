class Ringojs < Formula
  desc "CommonJS-based JavaScript runtime"
  homepage "https://ringojs.org"
  url "https://github.com/ringo/ringojs/releases/download/v4.0.0/ringojs-4.0.0.tar.gz"
  sha256 "9aea219fc6b4929a7949a34521cb96207073d29aa88f89f9a8833e31e84b14d5"
  license "Apache-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "6263e03d155e479441e8a23b3010d4d47fb96dad8661d05f46530317d2d0f54d"
  end

  depends_on "openjdk"

  def install
    rm Dir["bin/*.cmd"]
    rm_r "docker"

    # Ensure bottles are uniform. The `/usr/local` references are all in comments.
    inreplace %w[modules/fs.js modules/globals.js], "/usr/local", HOMEBREW_PREFIX

    bin.install Dir["bin/*"]
    libexec.install Dir["*"]
    java_env = Language::Java.overridable_java_home_env.merge(RINGO_HOME: libexec)
    bin.env_script_all_files libexec/"bin", java_env
  end

  test do
    (testpath/"test.js").write <<~JS
      var x = 40 + 2;
      console.assert(x === 42);
    JS
    system bin/"ringo", "test.js"
  end
end
