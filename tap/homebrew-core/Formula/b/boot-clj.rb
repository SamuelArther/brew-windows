class BootClj < Formula
  desc "Build tooling for Clojure"
  homepage "https://boot-clj.github.io/"
  url "https://github.com/boot-clj/boot/releases/download/2.8.3/boot.jar"
  sha256 "31f001988f580456b55a9462d95a8bf8b439956906c8aca65d3656206aa42ec7"
  license "EPL-1.0"
  revision 2

  bottle do
    sha256 cellar: :any, x86_64_windows: "ad7137c64b9fe574b81e58a2d50adf91c23535abb0cd514cee40c02f6f31d175"
  end

  depends_on "openjdk"

  def install
    libexec.install "boot.jar"
    (bin/"boot").write <<~SHELL
      #!/bin/bash
      export JAVA_HOME="${JAVA_HOME:-#{formula_opt_prefix("openjdk")}}"
      declare -a "options=($BOOT_JVM_OPTIONS)"
      exec "${JAVA_HOME}/bin/java" "${options[@]}" -Dboot.app.path="#{bin}/boot" -jar "#{libexec}/boot.jar" "$@"
    SHELL
  end

  test do
    system bin/"boot", "repl", "-e", "(System/exit 0)"
  end
end
