class Typescript < Formula
  desc "Language for application scale JavaScript development"
  homepage "https://www.typescriptlang.org/"
  url "https://registry.npmjs.org/@typescript/typescript-win32-x64/-/typescript-win32-x64-7.0.2.tgz"
  sha256 "61fc4e141d2bc687db580e71bbfa63b9c209f0310645d82ca1b457eb3a24fd19"
  license "Apache-2.0"
  revision 1

  livecheck do
    url "https://registry.npmjs.org/typescript/latest"
    regex(/"version"\s*:\s*"(\d+(?:\.\d+)+)"/i)
  end

  # TypeScript 7 is the native (Go) compiler. This is Microsoft's official Windows x64
  # npm package for it (what `npm install typescript` selects on Windows), so no Node.js is needed.

  def install
    install_tree
    windows_exe "libexec/lib/tsc.exe", "tsc"
  end

  test do
    (testpath/"test.ts").write <<~TYPESCRIPT
      class Test {
        greet() {
          return "Hello, world!";
        }
      };
      var test = new Test();
      document.body.innerHTML = test.greet();
    TYPESCRIPT

    system bin/"tsc", "test.ts"
    assert_path_exists testpath/"test.js", "test.js was not generated"
  end
end
