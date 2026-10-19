class KotlinLanguageServer < Formula
  desc "Intelligent Kotlin support for any editor/IDE using the Language Server Protocol"
  homepage "https://github.com/fwcd/kotlin-language-server"
  url "https://github.com/fwcd/kotlin-language-server/archive/refs/tags/1.3.13.tar.gz"
  sha256 "4cb346f989ef114f6073cb9401968a7dd27eb5cd96993fa6856203610a13f96e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d5933050c0922c37ddce1a3f00f8779252acce173e88f52f320d1d4a68cb2438"
  end

  depends_on "gradle" => :build
  depends_on "openjdk@21"

  # file permission literal patch
  patch do
    url "https://github.com/fwcd/kotlin-language-server/commit/a788e5f7b449dd701adc642c7cfb129f1895bd3e.patch?full_index=1"
    sha256 "cc9f6c68a09c76017099ffdd9bfe242a81b51221131bc33f3a7e2baa5bea6d01"
    type :backport
    resolves "https://github.com/fwcd/kotlin-language-server/pull/611"
  end

  def install
    ENV["JAVA_HOME"] = Language::Java.java_home("21")
    #  Remove Windows files
    rm "gradlew.bat"

    system "gradle", ":server:installDist", "-PjavaVersion=#{Formula["openjdk@21"].version.major}"

    libexec.install Dir["server/build/install/server/*"]

    (bin/"kotlin-language-server").write_env_script libexec/"bin/kotlin-language-server",
      Language::Java.overridable_java_home_env("21")
  end

  test do
    input =
      "Content-Length: 152\r\n" \
      "\r\n" \
      "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"" \
      "processId\":88075,\"rootUri\":null,\"capabilities\":{},\"trace\":\"ver" \
      "bose\",\"workspaceFolders\":null}}\r\n"

    output = pipe_output(bin/"kotlin-language-server", input, 0)

    assert_match(/^Content-Length: \d+/i, output)
  end
end
